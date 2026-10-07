using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.Sourcing;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 寻货端点（v1.35.0）：个人发布求购询价单 + 商户应答 + 选定解锁联系方式。
    /// 浏览公开（未登录可看 feed/详情），操作需登录；应答列表可见性分级——
    /// 发布人看全部明细（比价），商户只见自己的应答（防报价泄露），匿名只见应答数
    /// </summary>
    public static class SourcingEndpoints
    {
        /// <summary>
        /// 映射寻货端点（用户组 + Admin 管理组）
        /// </summary>
        public static void MapSourcingEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/sourcing");

            /// <summary>
            /// 寻货 feed：进行中需求分页（型号关键词 + 品牌/地区筛选；登录者带 isMine 标记）
            /// </summary>
            group.MapGet("/demands", async (
                [FromQuery] string? keyword,
                [FromQuery] bool onlyOpen,
                [FromQuery] string? brand,
                [FromQuery] string? region,
                [FromQuery] string? sort,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingDemandRepository demandRepository,
                HttpContext httpContext) =>
            {
                var userId = currentUser.UserId;
                // 改动说明（v1.4.0 信息架构收敛）：原 mineOnly 参数删除——发现页回归纯大厅，
                // "我的寻货"走独立端点 /my/demands（页面已在"我的"tab），feed 只服务公共浏览；
                // 同批新增 brand/region 筛选（自由文本包含匹配）与 sort 发布时间升降序
                // 公开列表先做惰性过期（一条 UPDATE 兜底僵尸单，读多写少成本可忽略）
                await demandRepository.ExpireOverdueAsync();
                var (items, total) = await demandRepository.GetListAsync(
                    onlyOpen ? SourcingDemand.StatusPublished : null, keyword, onlyOpen,
                    page <= 0 ? 1 : page, pageSize is > 0 and <= 50 ? pageSize : 20,
                    // 改动说明（v2.10.0）：公开大厅有效置顶排前
                    pinFirst: true,
                    brand: brand, region: region,
                    newestFirst: !string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase));
                var nowUtc = DateTime.UtcNow;
                return ApiResponseHelper.Ok(new
                {
                    items = items.Select(d => new
                    {
                        id = d.Id,
                        partNumber = d.PartNumber,
                        brand = d.Brand,
                        quantity = d.Quantity,
                        region = d.Region,
                        status = d.Status,
                        responseCount = d.ResponseCount,
                        createdAt = d.CreatedAt,
                        expiryAt = d.ExpiryAt,
                        // 改动说明（v2.10.0 寻货置顶）：置顶标记下发，大厅角标与"我的"页置顶按钮态消费
                        isPinned = d.PinnedUntil.HasValue && d.PinnedUntil.Value > nowUtc,
                        pinnedUntil = d.PinnedUntil,
                        // 改动说明（v2.12.0 商户名义发布）：发布方身份徽章
                        publisherMerchantId = d.PublisherMerchantId,
                        publisherMerchantName = d.PublisherMerchantName,
                        publisherType = d.IsMerchantPublished ? "merchant" : "individual",
                        isMine = userId.HasValue && d.PublisherUserId == userId.Value
                    }),
                    total
                }, httpContext: httpContext);
            })
            .WithName("GetSourcingFeed")
            .WithSummary("寻货列表")
            .WithDescription("寻货 feed（公开，型号关键词 + brand/region 包含筛选 + sort 升降序 + 置顶排序）");

            /// <summary>
            /// 寻货详情：按查看者身份分级返回应答数据与解锁的联系方式
            /// </summary>
            group.MapGet("/demands/{id:guid}", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingDemandRepository demandRepository,
                [FromServices] ISourcingResponseRepository responseRepository,
                [FromServices] IMerchantRepository merchantRepository,
                [FromServices] IMerchantMemberRepository memberRepository,
                [FromServices] IUserRepository userRepository,
                [FromServices] IMerchantBearingRepository merchantBearingRepository,
                [FromServices] IMerchantTaskRepository taskRepository,
                HttpContext httpContext) =>
            {
                var demand = await demandRepository.GetByIdAsync(id, httpContext.RequestAborted);
                if (demand == null)
                    return ApiResponseHelper.NotFound("寻货不存在", httpContext: httpContext);
                if (demand.TryExpire())
                    await demandRepository.UpdateAsync(demand, httpContext.RequestAborted);

                var userId = currentUser.UserId;
                var isPublisher = userId.HasValue && demand.PublisherUserId == userId.Value;
                // 当前商户在本需求上的应答（任意在职成员可见自己商户的应答详情）
                SourcingResponse? myResponse = null;
                if (userId.HasValue && currentUser.CurrentMerchantId.HasValue)
                {
                    myResponse = await responseRepository.GetByDemandAndMerchantAsync(
                        demand.Id, currentUser.CurrentMerchantId.Value, httpContext.RequestAborted);
                }

                var responses = await responseRepository.GetByDemandAsync(demand.Id, httpContext.RequestAborted);

                // 发布人视图：全部应答明细（商户名/认证/报价，比价依据）+ 被选商户联系方式
                List<object>? fullResponses = null;
                string? selectedMerchantContact = null;
                if (isPublisher)
                {
                    fullResponses = new List<object>();
                    foreach (var r in responses)
                    {
                        var merchant = await merchantRepository.GetByIdAsync(r.MerchantId, httpContext.RequestAborted);
                        if (r.Status == SourcingResponse.StatusAdopted && merchant != null)
                        {
                            // 选定后解锁被选商户联系方式（手机优先，回退座机——与商家主页 merchantDetail 展示口径一致）
                            selectedMerchantContact = merchant.Contact?.Mobile ?? merchant.Contact?.Phone;
                        }
                        var items = await responseRepository.GetItemsAsync(r.Id, httpContext.RequestAborted);
                        // 改动说明（v1.5.0 证据力体系 P1）：逐条透出商家实力摘要——
                        // 在售商品数（现货凭证）+ 集体任务累计达成（历史履约能力）+ 公司名，
                        // 发布人选定前的判断依据从"自述文本"升级为"结构化证据"
                        var onSaleCount = await merchantBearingRepository.CountOnSaleAsync(r.MerchantId, httpContext.RequestAborted);
                        var completedTaskCount = await taskRepository.CountCompletionsAsync(r.MerchantId, httpContext.RequestAborted);
                        fullResponses.Add(new
                        {
                            id = r.Id,
                            merchantId = r.MerchantId,
                            merchantName = merchant?.Name,
                            isVerified = merchant?.IsVerified ?? false,
                            companyName = merchant?.CompanyName,
                            onSaleCount,
                            completedTaskCount,
                            // v1.5.0 多行标书：应答型号行（发布人逐行挑选依据）
                            items = items.Select(i => new
                            {
                                id = i.Id,
                                partNumber = i.PartNumber,
                                bearingId = i.BearingId,
                                price = i.Price,
                                stock = i.Stock,
                                leadTime = i.LeadTime
                            }),
                            remark = r.Remark,
                            status = r.Status,
                            createdAt = r.CreatedAt
                        });
                    }
                }

                // 被选商户视图：解锁发布方联系方式。改动说明（v2.12.0 商户名义发布）：
                //   商户单给商户公开电话（手机优先回退座机，与 selectedMerchantContact 口径对称，
                //   不暴露经办人个人手机）；个人单维持发布人注册手机
                string? publisherContact = null;
                if (myResponse?.Status == SourcingResponse.StatusAdopted && userId.HasValue)
                {
                    if (demand.IsMerchantPublished)
                    {
                        var pubMerchant = await merchantRepository.GetByIdAsync(demand.PublisherMerchantId!.Value, httpContext.RequestAborted);
                        publisherContact = pubMerchant?.Contact?.Mobile ?? pubMerchant?.Contact?.Phone;
                    }
                    else
                    {
                        var publisher = await userRepository.GetByIdAsync(demand.PublisherUserId, httpContext.RequestAborted);
                        publisherContact = publisher?.Mobile;
                    }
                }

                // v1.5.0 多行标书：商户视角自己的应答也透出型号行（查看/修改应答时逐行回显）
                var myResponseItems = myResponse == null
                    ? null
                    : (await responseRepository.GetItemsAsync(myResponse.Id, httpContext.RequestAborted))
                        .Select(i => new
                        {
                            id = i.Id,
                            partNumber = i.PartNumber,
                            bearingId = i.BearingId,
                            price = i.Price,
                            stock = i.Stock,
                            leadTime = i.LeadTime
                        });

                return ApiResponseHelper.Ok(new
                {
                    id = demand.Id,
                    partNumber = demand.PartNumber,
                    // 改动说明（v1.36.0）：透出 bearingId 供前端"我的在售同款"应答预填精确匹配
                    bearingId = demand.BearingId,
                    brand = demand.Brand,
                    quantity = demand.Quantity,
                    expectedDelivery = demand.ExpectedDelivery,
                    region = demand.Region,
                    description = demand.Description,
                    status = demand.Status,
                    responseCount = demand.ResponseCount,
                    createdAt = demand.CreatedAt,
                    expiryAt = demand.ExpiryAt,
                    closedAt = demand.ClosedAt,
                    isPublisher,
                    // 改动说明（v2.12.0 商户名义发布）：发布方身份透出（前端徽章/跳转用）
                    publisherMerchantId = demand.PublisherMerchantId,
                    publisherMerchantName = demand.PublisherMerchantName,
                    publisherType = demand.IsMerchantPublished ? "merchant" : "individual",
                    // 发布人才见全量应答；其他人（含商户）只见自己的，其余仅计数（防报价泄露）
                    responses = fullResponses,
                    myResponse = myResponse == null ? null : new
                    {
                        id = myResponse.Id,
                        // v1.5.0 多行标书：商户视角应答的型号行（含引用在售的 bearingId）
                        items = myResponseItems,
                        remark = myResponse.Remark,
                        status = myResponse.Status,
                        createdAt = myResponse.CreatedAt
                    },
                    selectedMerchantContact,
                    publisherContact
                }, httpContext: httpContext);
            })
            .WithName("GetSourcingDetail")
            .WithSummary("寻货详情")
            .WithDescription("需求全文+分级应答视图+选定后解锁的联系方式");

            /// <summary>
            /// 发布寻货需求（登录用户；免费额度→积分加量→硬上限，NeedPoints 以 400 文案回传）
            /// </summary>
            group.MapPost("/demands", async (
                [FromBody] PublishDemandRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var demandId = await mediator.Send(new PublishDemandCommand(
                    currentUser.UserId.Value, request.PartNumber, request.BearingId, request.Brand,
                    request.Quantity, request.ExpectedDelivery, request.Region, request.Description,
                    request.UsePoints, request.MerchantId), httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { id = demandId }, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("PublishSourcingDemand")
            .WithSummary("发布寻货")
            .WithDescription("个人用户发布求购询价单（额度模型：免费+积分加量+硬上限）");

            /// <summary>
            /// 应答寻货（当前商户；重复应答=更新；报价等可选、说明必填）
            /// </summary>
            group.MapPost("/demands/{id:guid}/respond", async (
                Guid id,
                [FromBody] RespondDemandRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.Unauthorized("请先选择当前商户", httpContext: httpContext);

                await mediator.Send(new RespondDemandCommand(
                    currentUser.UserId.Value, currentUser.CurrentMerchantId.Value, id,
                    request.Items, request.Remark, request.UsePoints),
                    httpContext.RequestAborted);
                return ApiResponseHelper.Ok("应答成功", httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("RespondSourcingDemand")
            .WithSummary("应答寻货")
            .WithDescription("商户对进行中寻货提交报价应答（一商户一条，可更新）");

            /// <summary>
            /// 撤销应答（当前商户；仅待处理可撤；撤后需求回到未应答——招投标"开标前撤标"）
            /// </summary>
            group.MapDelete("/demands/{id:guid}/respond", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.Unauthorized("请先选择当前商户", httpContext: httpContext);

                await mediator.Send(new CancelResponseCommand(
                    currentUser.UserId.Value, currentUser.CurrentMerchantId.Value, id),
                    httpContext.RequestAborted);
                return ApiResponseHelper.Ok("应答已撤销", httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("CancelSourcingResponse")
            .WithSummary("撤销应答")
            .WithDescription("商户撤回本商户对该需求的待处理应答，需求回到未应答状态；当日额度不退还");

            /// <summary>
            /// 选定应答关闭寻货（发布人；触发双方联系方式解锁与通知）
            /// </summary>
            group.MapPost("/demands/{id:guid}/select", async (
                Guid id,
                [FromBody] SelectResponseRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new SelectResponseCommand(currentUser.UserId.Value, id, request.ResponseId),
                    httpContext.RequestAborted);
                return ApiResponseHelper.Ok("已选定，双方联系方式已解锁", httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("SelectSourcingResponse")
            .WithSummary("选定应答")
            .WithDescription("发布人确认合作商户，需求关闭并解锁双方联系方式");

            /// <summary>
            /// 取消寻货（发布人；通知全体应答商户）
            /// </summary>
            group.MapPost("/demands/{id:guid}/cancel", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new CancelDemandCommand(currentUser.UserId.Value, id), httpContext.RequestAborted);
                return ApiResponseHelper.Ok("寻货已取消", httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("CancelSourcingDemand")
            .WithSummary("取消寻货")
            .WithDescription("发布人主动取消进行中的寻货");

            /// <summary>
            /// 我发布的寻货（个人维度，全部状态倒序）
            /// </summary>
            group.MapGet("/my/demands", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingDemandRepository demandRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await demandRepository.ExpireOverdueAsync();
                var items = await demandRepository.GetByPublisherAsync(currentUser.UserId.Value, httpContext.RequestAborted);
                var nowPinned = DateTime.UtcNow;
                return ApiResponseHelper.Ok(items.Select(d => new
                {
                    id = d.Id,
                    partNumber = d.PartNumber,
                    brand = d.Brand,
                    quantity = d.Quantity,
                    status = d.Status,
                    responseCount = d.ResponseCount,
                    createdAt = d.CreatedAt,
                    expiryAt = d.ExpiryAt,
                    // 改动说明（v2.10.0 寻货置顶）："我的"页置顶按钮态数据源
                    isPinned = d.PinnedUntil.HasValue && d.PinnedUntil.Value > nowPinned,
                    pinnedUntil = d.PinnedUntil,
                    // 改动说明（v2.12.0 商户名义发布）："我的寻货"卡片显示发布身份
                    publisherMerchantId = d.PublisherMerchantId,
                    publisherMerchantName = d.PublisherMerchantName,
                    publisherType = d.IsMerchantPublished ? "merchant" : "individual"
                }), httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetMySourcingDemands")
            .WithSummary("我发布的寻货");

            /// <summary>
            /// 商户名义发布的寻货列表（v2.12.0 商户工作台"寻货管理-我发布的"）：
            /// 按当前商户（X-Merchant-Id）查 PublisherMerchantId=该商户 的全状态单，
            /// 在职成员均可见（含经办人标记，管理操作仍限经办人本人——前端按 isMine 出按钮）
            /// </summary>
            group.MapGet("/merchant/demands", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantMemberRepository memberRepository,
                [FromServices] ISourcingDemandRepository demandRepository,
                [FromServices] IUserRepository userRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // 在职成员校验（与应答同款口径，员工可看本店需求单）
                var member = await memberRepository.GetActiveByUserAndMerchantAsync(
                    currentUser.UserId.Value, currentUser.CurrentMerchantId.Value, httpContext.RequestAborted);
                if (member is null)
                    return ApiResponseHelper.Forbidden("非该商户在职成员", httpContext);

                await demandRepository.ExpireOverdueAsync();
                var items = await demandRepository.GetByPublisherMerchantAsync(
                    currentUser.CurrentMerchantId.Value, httpContext.RequestAborted);
                var nowPinned = DateTime.UtcNow;
                var list = new List<object>();
                foreach (var d in items)
                {
                    // 经办人昵称（列表最多 100 条，逐条取用户行可接受——与 my/responses 需求快照同款）
                    var publisher = await userRepository.GetByIdAsync(d.PublisherUserId, httpContext.RequestAborted);
                    list.Add(new
                    {
                        id = d.Id,
                        partNumber = d.PartNumber,
                        brand = d.Brand,
                        quantity = d.Quantity,
                        status = d.Status,
                        responseCount = d.ResponseCount,
                        createdAt = d.CreatedAt,
                        expiryAt = d.ExpiryAt,
                        isPinned = d.PinnedUntil.HasValue && d.PinnedUntil.Value > nowPinned,
                        pinnedUntil = d.PinnedUntil,
                        // 经办人信息：管理按钮仅本人可见，其他成员提示"由发布人操作"
                        publisherName = publisher?.Nickname ?? "已注销用户",
                        isMine = d.PublisherUserId == currentUser.UserId.Value
                    });
                }
                return ApiResponseHelper.Ok(list, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetMerchantSourcingDemands")
            .WithSummary("商户名义发布的寻货列表");

            /// <summary>
            /// 批量删除寻货需求（v2.12.0 列表删除）：经办人软删自己的终态单（左滑单删=ids 传一个）；
            /// 进行中拒删计入 skipped（须先取消走通知流程）；数据与应答方视图保留
            /// </summary>
            group.MapPost("/demands/batch-delete", async (
                BatchDeleteDemandRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var (deleted, skipped) = await mediator.Send(new BatchDeleteDemandsCommand(currentUser.UserId.Value, request.Ids), httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { deleted, skipped }, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("BatchDeleteSourcingDemands")
            .WithSummary("批量删除寻货")
            .WithDescription("软删终态单（仅经办人本人），进行中拒删计 skipped");

            /// <summary>
            /// 当前商户的应答记录（商家维度，含需求快照与状态）
            /// </summary>
            group.MapGet("/my/responses", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingResponseRepository responseRepository,
                [FromServices] ISourcingDemandRepository demandRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.Ok(Array.Empty<object>(), httpContext: httpContext);

                var responses = await responseRepository.GetByMerchantAsync(
                    currentUser.CurrentMerchantId.Value, httpContext.RequestAborted);
                var result = new List<object>();
                foreach (var r in responses)
                {
                    // 需求快照（应答列表最多 100 条，逐条取详情行可接受）
                    var demand = await demandRepository.GetByIdAsync(r.DemandId, httpContext.RequestAborted);
                    // v1.5.0 多行标书：商家应答记录透出型号行
                    var items = await responseRepository.GetItemsAsync(r.Id, httpContext.RequestAborted);
                    result.Add(new
                    {
                        id = r.Id,
                        demandId = r.DemandId,
                        partNumber = demand?.PartNumber,
                        demandStatus = demand?.Status,
                        items = items.Select(i => new
                        {
                            id = i.Id,
                            partNumber = i.PartNumber,
                            bearingId = i.BearingId,
                            price = i.Price,
                            stock = i.Stock,
                            leadTime = i.LeadTime
                        }),
                        remark = r.Remark,
                        status = r.Status,
                        createdAt = r.CreatedAt
                    });
                }
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetMySourcingResponses")
            .WithSummary("商户寻货应答记录");

            /// <summary>
            /// 额度聚合（v1.34.0 额度可见化）：发布/应答的免费额度、今日已用、硬上限、
            /// 积分单价与当前积分余额一次返回。
            /// 改动说明：此前额度规则"撞墙才可见"（超限报错才提示花积分），前端无法前置
            /// 展示额度条与按钮三态；口径与两个 Command handler 完全同源（同配置键/同规则键/
            /// 同计数方法），保证展示与实际判定不分叉
            /// </summary>
            group.MapGet("/quota", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingDemandRepository demandRepository,
                [FromServices] ISourcingResponseRepository responseRepository,
                [FromServices] ISystemConfigRepository configRepository,
                [FromServices] IPointGrantRuleRepository ruleRepository,
                [FromServices] IPointAccountRepository pointAccountRepository,
                [FromServices] IMerchantGradeService grades,
                [FromServices] IMerchantRepository merchants,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var userId = currentUser.UserId.Value;
                var publishRule = await ruleRepository.GetEnabledByTypeAsync("sourcing_publish_bonus", httpContext.RequestAborted);
                var respondRule = await ruleRepository.GetEnabledByTypeAsync("sourcing_respond_bonus", httpContext.RequestAborted);
                var account = await pointAccountRepository.GetByUserIdAsync(userId, httpContext.RequestAborted);

                // 发布额度（个人维度）；应答额度（当前商户维度，未入驻商户返回 0 已用）
                // v2.5.0 商家经济：展示口径与 Command handler 同源——发布按成员最佳商家加成，应答按当前商户等级加成
                var bestMerchant = await grades.GetBestForUserAsync(userId, httpContext.RequestAborted);
                var currentGrade = 0;
                if (currentUser.CurrentMerchantId.HasValue)
                {
                    var gm = await merchants.GetByIdAsync(currentUser.CurrentMerchantId.Value, httpContext.RequestAborted);
                    if (gm != null && gm.Status == Domain.Enums.MerchantStatus.Active) currentGrade = (int)gm.Grade;
                }
                var publishToday = await demandRepository.CountPublishedTodayAsync(userId, httpContext.RequestAborted);
                var respondToday = currentUser.CurrentMerchantId.HasValue
                    ? await responseRepository.CountRespondedTodayAsync(currentUser.CurrentMerchantId.Value, httpContext.RequestAborted)
                    : 0;

                return ApiResponseHelper.Ok(new
                {
                    publish = new
                    {
                        freeLimit = await SourcingConfigReader.GetIntAsync(configRepository, "Sourcing.FreePublishPerDay", 3) + MerchantBuffs.PublishQuotaBonus(bestMerchant?.Grade ?? 0),
                        todayUsed = publishToday,
                        hardLimit = publishRule?.DailyLimit ?? 10,
                        pointsPrice = publishRule?.Amount ?? 20
                    },
                    respond = new
                    {
                        freeLimit = await SourcingConfigReader.GetIntAsync(configRepository, "Sourcing.FreeRespondPerDay", 20) + MerchantBuffs.RespondQuotaBonus(currentGrade),
                        todayUsed = respondToday,
                        hardLimit = respondRule?.DailyLimit ?? 50,
                        pointsPrice = respondRule?.Amount ?? 20
                    },
                    balance = account?.Balance ?? 0
                }, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetSourcingQuota")
            .WithSummary("寻货额度聚合")
            .WithDescription("发布/应答免费额度、今日已用、硬上限、积分单价与余额（额度条数据源）");

            /// <summary>
            /// 我的在售同款（v1.36.0 应答预填）：当前商户对该寻货型号的在售/补货中商品条目，
            /// 应答表单据此预填报价/库存/交期——商户维护的商品数据第一次直接变成应答回报。
            /// 匹配口径：bearingId 精确优先；无 id 时按型号文本精确查主数据再匹配
            /// </summary>
            group.MapGet("/my-offering", async (
                [FromQuery] Guid? bearingId,
                [FromQuery] string? partNumber,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantBearingRepository merchantBearingRepository,
                [FromServices] IBearingRepository bearingRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.Ok(new { found = false }, httpContext: httpContext);

                var bid = bearingId;
                if (bid == null && !string.IsNullOrWhiteSpace(partNumber))
                {
                    var bearing = await bearingRepository.GetByPartNumberAsync(partNumber.Trim(), httpContext.RequestAborted);
                    bid = bearing?.Id;
                }
                if (bid == null)
                    return ApiResponseHelper.Ok(new { found = false }, httpContext: httpContext);

                var relations = await merchantBearingRepository.GetByBearingAsync(bid.Value, httpContext.RequestAborted);
                var mine = relations.Where(mb => mb.MerchantId == currentUser.CurrentMerchantId.Value).ToList();
                // 在售条目优先，其次补货中条目（应答预填取最接近"现在能供"的状态）
                var offering = mine.FirstOrDefault(mb => mb.IsOnSale)
                    ?? mine.FirstOrDefault(mb => mb.IsRestocking);
                if (offering == null)
                    return ApiResponseHelper.Ok(new { found = false }, httpContext: httpContext);

                return ApiResponseHelper.Ok(new
                {
                    found = true,
                    isOnSale = offering.IsOnSale,
                    isRestocking = offering.IsRestocking,
                    price = offering.NumericPrice ?? (decimal?)null,
                    priceDescription = offering.PriceDescription,
                    stock = offering.StockDescription,
                    minOrder = offering.MinOrderDescription,
                    restockEta = offering.RestockEta,
                    remarks = offering.Remarks
                }, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetMyOffering")
            .WithSummary("我的在售同款（应答预填）")
            .WithDescription("当前商户对该寻货型号的在售/补货中条目，应答表单预填数据源");

            /// <summary>
            /// 需求信号（v1.36.0 反向导购）：当前商户在售型号中，哪些正被寻货且尚无应答——
            /// "你卖的型号有人要"横幅数据源。口径：进行中未应答寻货按型号聚合 × 商户在售型号集合
            /// </summary>
            group.MapGet("/opportunities", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingDemandRepository demandRepository,
                [FromServices] IMerchantBearingRepository merchantBearingRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.Ok(Array.Empty<object>(), httpContext: httpContext);

                // 在售型号集合（含补货中——补货中的型号收到需求同样值得商家知道）
                var onSale = await merchantBearingRepository.GetOnSaleByMerchantAsync(
                    currentUser.CurrentMerchantId.Value, httpContext.RequestAborted);
                var myParts = onSale
                    .Where(mb => mb.Bearing != null)
                    .Select(mb => (mb.Bearing!.PartNumber ?? "").Trim().ToUpperInvariant())
                    .Where(p => p.Length > 0)
                    .ToHashSet();
                if (myParts.Count == 0)
                    return ApiResponseHelper.Ok(Array.Empty<object>(), httpContext: httpContext);

                // 进行中未应答寻货（近 200 条内存聚合，冷启动量级足够；上量后改 SQL 聚合）
                var (items, _) = await demandRepository.GetListAsync(
                    status: SourcingDemand.StatusPublished, keyword: null, onlyOpen: true,
                    page: 1, pageSize: 200, cancellationToken: httpContext.RequestAborted);
                var opportunities = items
                    .Where(d => d.ResponseCount == 0)
                    .GroupBy(d => (d.PartNumber ?? "").Trim().ToUpperInvariant())
                    .Where(g => myParts.Contains(g.Key))
                    .Select(g => new
                    {
                        partNumber = g.First().PartNumber,
                        demandCount = g.Count(),
                        latestAt = g.Max(d => d.CreatedAt)
                    })
                    .OrderByDescending(x => x.demandCount)
                    .Take(10)
                    .ToList();
                return ApiResponseHelper.Ok(opportunities, httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetSourcingOpportunities")
            .WithSummary("需求信号（反向导购）")
            .WithDescription("商户在售型号中被寻货且未应答的聚合清单（商家寻货页横幅数据源）");

            // ============ Admin 治理端点 ============
            var adminGroup = app.MapGroup("/api/admin/sourcing").RequireAuthorization();

            /// <summary>
            /// Admin 寻货列表：全状态分页（状态筛选+型号关键词），治理视图
            /// </summary>
            adminGroup.MapGet("/demands", async (
                [FromQuery] int? status,
                [FromQuery] string? keyword,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromServices] ISourcingDemandRepository demandRepository,
                [FromServices] IUserRepository userRepository,
                HttpContext httpContext) =>
            {
                await demandRepository.ExpireOverdueAsync();
                var (items, total) = await demandRepository.GetListAsync(status, keyword, false,
                    page <= 0 ? 1 : page, pageSize is > 0 and <= 100 ? pageSize : 20,
                    cancellationToken: httpContext.RequestAborted);
                var list = new List<object>();
                foreach (var d in items)
                {
                    var publisher = await userRepository.GetByIdAsync(d.PublisherUserId, httpContext.RequestAborted);
                    list.Add(new
                    {
                        id = d.Id,
                        partNumber = d.PartNumber,
                        brand = d.Brand,
                        quantity = d.Quantity,
                        region = d.Region,
                        status = d.Status,
                        responseCount = d.ResponseCount,
                        publisherName = publisher?.Nickname ?? "已注销用户",
                        createdAt = d.CreatedAt,
                        expiryAt = d.ExpiryAt,
                        closedAt = d.ClosedAt
                    });
                }
                return ApiResponseHelper.Ok(new { items = list, total }, httpContext: httpContext);
            })
            .WithName("AdminGetSourcingDemands")
            .WithSummary("寻货列表（Admin）")
            .WithDescription("全状态分页查询与型号搜索（sourcing.view）")
            .RequirePermission("sourcing.view");

            /// <summary>
            /// Admin 寻货详情：需求全文 + 全部应答明细 + 发布人联系方式
            /// （公开详情端点对非发布人隐藏应答，治理审核需要全量视图，故独立端点）
            /// </summary>
            adminGroup.MapGet("/demands/{id:guid}", async (
                Guid id,
                [FromServices] ISourcingDemandRepository demandRepository,
                [FromServices] ISourcingResponseRepository responseRepository,
                [FromServices] IMerchantRepository merchantRepository,
                [FromServices] IUserRepository userRepository,
                HttpContext httpContext) =>
            {
                var demand = await demandRepository.GetByIdAsync(id, httpContext.RequestAborted);
                if (demand == null)
                    return ApiResponseHelper.NotFound("寻货不存在", httpContext: httpContext);

                var publisher = await userRepository.GetByIdAsync(demand.PublisherUserId, httpContext.RequestAborted);
                var responses = await responseRepository.GetByDemandAsync(demand.Id, httpContext.RequestAborted);
                var list = new List<object>();
                foreach (var r in responses)
                {
                    var merchant = await merchantRepository.GetByIdAsync(r.MerchantId, httpContext.RequestAborted);
                    // v1.5.0 多行标书：Admin 审核视图同样看型号行
                    var items = await responseRepository.GetItemsAsync(r.Id, httpContext.RequestAborted);
                    list.Add(new
                    {
                        id = r.Id,
                        merchantId = r.MerchantId,
                        merchantName = merchant?.Name,
                        isVerified = merchant?.IsVerified ?? false,
                        items = items.Select(i => new
                        {
                            id = i.Id,
                            partNumber = i.PartNumber,
                            bearingId = i.BearingId,
                            price = i.Price,
                            stock = i.Stock,
                            leadTime = i.LeadTime
                        }),
                        remark = r.Remark,
                        status = r.Status,
                        createdAt = r.CreatedAt
                    });
                }
                return ApiResponseHelper.Ok(new
                {
                    id = demand.Id,
                    partNumber = demand.PartNumber,
                    brand = demand.Brand,
                    quantity = demand.Quantity,
                    expectedDelivery = demand.ExpectedDelivery,
                    region = demand.Region,
                    description = demand.Description,
                    status = demand.Status,
                    responseCount = demand.ResponseCount,
                    createdAt = demand.CreatedAt,
                    expiryAt = demand.ExpiryAt,
                    closedAt = demand.ClosedAt,
                    publisherName = publisher?.Nickname ?? "已注销用户",
                    publisherMobile = publisher?.Mobile,
                    responses = list
                }, httpContext: httpContext);
            })
            .WithName("AdminGetSourcingDetail")
            .WithSummary("寻货详情（Admin）")
            .WithDescription("需求全文+全部应答+发布人联系方式（sourcing.view）")
            .RequirePermission("sourcing.view");

            /// <summary>
            /// Admin 下架寻货（违规治理；通知发布人附原因）
            /// </summary>
            adminGroup.MapPost("/demands/{id:guid}/takedown", async (
                Guid id,
                [FromBody] TakeDownRequest? request,
                [FromServices] MediatR.IMediator mediator,
                HttpContext httpContext) =>
            {
                await mediator.Send(new TakeDownDemandCommand(id, request?.Reason), httpContext.RequestAborted);
                return ApiResponseHelper.Ok("已下架", httpContext: httpContext);
            })
            .WithName("AdminTakeDownSourcingDemand")
            .WithSummary("下架寻货（Admin）")
            .WithDescription("违规寻货软下架并通知发布人（sourcing.manage）")
            .RequirePermission("sourcing.manage");
        }
    }

    /// <summary>发布寻货请求体</summary>
    /// <param name="PartNumber">型号（必填）</param>
    /// <param name="BearingId">关联平台轴承（可空）</param>
    /// <param name="Brand">期望品牌</param>
    /// <param name="Quantity">数量描述</param>
    /// <param name="ExpectedDelivery">交期描述</param>
    /// <param name="Region">地区</param>
    /// <param name="Description">补充说明</param>
    /// <param name="UsePoints">免费额度用尽后确认花积分</param>
    public record PublishDemandRequest(string PartNumber, Guid? BearingId, string? Brand, string? Quantity,
        string? ExpectedDelivery, string? Region, string? Description, bool UsePoints, Guid? MerchantId = null);

    /// <summary>应答寻货请求体（v1.5.0 多行标书：报价/库存/交期按行携带）</summary>
    /// <param name="Items">应答型号行（至少一行，每行型号必填）</param>
    /// <param name="Remark">应答说明（必填）</param>
    /// <param name="UsePoints">免费额度用尽后确认花积分</param>
    public record RespondDemandRequest(List<SourcingResponseItemInput> Items, string Remark, bool UsePoints);

    /// <summary>选定应答请求体</summary>
    /// <param name="ResponseId">被选定的应答 ID</param>
    public record SelectResponseRequest(Guid ResponseId);

    /// <summary>下架请求体</summary>
    /// <param name="Reason">下架原因（透传发布人）</param>
    public record TakeDownRequest(string? Reason);

    /// <summary>批量删除寻货请求（v2.12.0 列表删除；左滑单删=ids 传一个）</summary>
    public record BatchDeleteDemandRequest(List<Guid> Ids);
}
