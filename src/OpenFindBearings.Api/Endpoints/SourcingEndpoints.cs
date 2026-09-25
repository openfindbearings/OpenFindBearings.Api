using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.Sourcing;
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
            /// 寻货 feed：进行中需求分页（型号关键词搜索；登录者带 isMine 标记）
            /// </summary>
            group.MapGet("/demands", async (
                [FromQuery] string? keyword,
                [FromQuery] bool onlyOpen,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingDemandRepository demandRepository,
                HttpContext httpContext) =>
            {
                // 公开列表先做惰性过期（一条 UPDATE 兜底僵尸单，读多写少成本可忽略）
                await demandRepository.ExpireOverdueAsync();
                var (items, total) = await demandRepository.GetListAsync(
                    onlyOpen ? SourcingDemand.StatusPublished : null, keyword, onlyOpen,
                    page <= 0 ? 1 : page, pageSize is > 0 and <= 50 ? pageSize : 20);
                var userId = currentUser.UserId;
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
                        isMine = userId.HasValue && d.PublisherUserId == userId.Value
                    }),
                    total
                }, httpContext: httpContext);
            })
            .WithName("GetSourcingFeed")
            .WithSummary("寻货列表")
            .WithDescription("进行中的寻货需求 feed（公开浏览，支持型号搜索）");

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
                            // 选定后解锁被选商户联系方式（电话优先，回退手机）
                            selectedMerchantContact = merchant.Contact?.Phone ?? merchant.Contact?.Mobile;
                        }
                        fullResponses.Add(new
                        {
                            id = r.Id,
                            merchantId = r.MerchantId,
                            merchantName = merchant?.Name,
                            isVerified = merchant?.IsVerified ?? false,
                            price = r.Price,
                            stock = r.Stock,
                            leadTime = r.LeadTime,
                            remark = r.Remark,
                            status = r.Status,
                            createdAt = r.CreatedAt
                        });
                    }
                }

                // 被选商户视图：解锁发布人手机号（仅被选中的那条应答的商户）
                string? publisherContact = null;
                if (myResponse?.Status == SourcingResponse.StatusAdopted && userId.HasValue)
                {
                    var publisher = await userRepository.GetByIdAsync(demand.PublisherUserId, httpContext.RequestAborted);
                    publisherContact = publisher?.Mobile;
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
                    isPublisher,
                    // 发布人才见全量应答；其他人（含商户）只见自己的，其余仅计数（防报价泄露）
                    responses = fullResponses,
                    myResponse = myResponse == null ? null : new
                    {
                        id = myResponse.Id,
                        price = myResponse.Price,
                        stock = myResponse.Stock,
                        leadTime = myResponse.LeadTime,
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
                    request.UsePoints), httpContext.RequestAborted);
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
                    request.Price, request.Stock, request.LeadTime, request.Remark, request.UsePoints),
                    httpContext.RequestAborted);
                return ApiResponseHelper.Ok("应答成功", httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("RespondSourcingDemand")
            .WithSummary("应答寻货")
            .WithDescription("商户对进行中寻货提交报价应答（一商户一条，可更新）");

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
                return ApiResponseHelper.Ok(items.Select(d => new
                {
                    id = d.Id,
                    partNumber = d.PartNumber,
                    brand = d.Brand,
                    quantity = d.Quantity,
                    status = d.Status,
                    responseCount = d.ResponseCount,
                    createdAt = d.CreatedAt,
                    expiryAt = d.ExpiryAt
                }), httpContext: httpContext);
            })
            .RequireAuthorization()
            .WithName("GetMySourcingDemands")
            .WithSummary("我发布的寻货");

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
                    result.Add(new
                    {
                        id = r.Id,
                        demandId = r.DemandId,
                        partNumber = demand?.PartNumber,
                        demandStatus = demand?.Status,
                        price = r.Price,
                        stock = r.Stock,
                        leadTime = r.LeadTime,
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
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var userId = currentUser.UserId.Value;
                var publishRule = await ruleRepository.GetEnabledByTypeAsync("sourcing_publish_bonus", httpContext.RequestAborted);
                var respondRule = await ruleRepository.GetEnabledByTypeAsync("sourcing_respond_bonus", httpContext.RequestAborted);
                var account = await pointAccountRepository.GetByUserIdAsync(userId, httpContext.RequestAborted);

                // 发布额度（个人维度）；应答额度（当前商户维度，未入驻商户返回 0 已用）
                var publishToday = await demandRepository.CountPublishedTodayAsync(userId, httpContext.RequestAborted);
                var respondToday = currentUser.CurrentMerchantId.HasValue
                    ? await responseRepository.CountRespondedTodayAsync(currentUser.CurrentMerchantId.Value, httpContext.RequestAborted)
                    : 0;

                return ApiResponseHelper.Ok(new
                {
                    publish = new
                    {
                        freeLimit = await SourcingConfigReader.GetIntAsync(configRepository, "Sourcing.FreePublishPerDay", 3),
                        todayUsed = publishToday,
                        hardLimit = publishRule?.DailyLimit ?? 10,
                        pointsPrice = publishRule?.Amount ?? 20
                    },
                    respond = new
                    {
                        freeLimit = await SourcingConfigReader.GetIntAsync(configRepository, "Sourcing.FreeRespondPerDay", 20),
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
                    page <= 0 ? 1 : page, pageSize is > 0 and <= 100 ? pageSize : 20, httpContext.RequestAborted);
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
                    list.Add(new
                    {
                        id = r.Id,
                        merchantId = r.MerchantId,
                        merchantName = merchant?.Name,
                        isVerified = merchant?.IsVerified ?? false,
                        price = r.Price,
                        stock = r.Stock,
                        leadTime = r.LeadTime,
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
        string? ExpectedDelivery, string? Region, string? Description, bool UsePoints);

    /// <summary>应答寻货请求体</summary>
    /// <param name="Price">报价单价（可选）</param>
    /// <param name="Stock">库存描述（可选）</param>
    /// <param name="LeadTime">交期描述（可选）</param>
    /// <param name="Remark">应答说明（必填）</param>
    /// <param name="UsePoints">免费额度用尽后确认花积分</param>
    public record RespondDemandRequest(decimal? Price, string? Stock, string? LeadTime, string Remark, bool UsePoints);

    /// <summary>选定应答请求体</summary>
    /// <param name="ResponseId">被选定的应答 ID</param>
    public record SelectResponseRequest(Guid ResponseId);

    /// <summary>下架请求体</summary>
    /// <param name="Reason">下架原因（透传发布人）</param>
    public record TakeDownRequest(string? Reason);
}
