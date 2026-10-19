using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 撮合会话端点（v2.18.0 T1 聚焦式会话）：绑定"寻货单 × 商户"的私域对话。
    /// 会话由首条消息懒创建；双方身份由服务端裁决（需求方方 / 商家方），
    /// 商家方须为受邀商户或已应答商户，防止任意商户对任意单开聊。
    /// 需求单关闭/取消/过期后会话只读保留（历史可查，不可再发）。
    /// </summary>
    public static class ConversationEndpoints
    {
        /// <summary>单页消息条数上限（防止一次拉全量会话历史）</summary>
        private const int MaxMessagePageSize = 50;

        /// <summary>
        /// 映射撮合会话端点（挂在 /api/sourcing/conversations，需登录）
        /// </summary>
        public static void MapConversationEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/sourcing/conversations").RequireAuthorization();

            /// <summary>
            /// 会话列表：需求方视角与商家方视角合并，按最后活跃倒序（角色/对端/未读/单据状态）
            /// </summary>
            group.MapGet("", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingConversationRepository conversations,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] ISourcingDemandRepository demands,
                [FromServices] IMerchantRepository merchants,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);
                var userId = currentUser.UserId.Value;
                var take = pageSize <= 0 ? 20 : Math.Min(pageSize, 50);
                var skip = page <= 1 ? 0 : (page - 1) * take;

                // 两视角各自取够 skip+take 条后合并排序再切片（当前会话量级小，内存合并足够；
                // 若将来量大，可改为 UNION 查询或统一会话参与人表）
                var merchantIds = (await members.GetActiveByUserIdAsync(userId, httpContext.RequestAborted))
                    .Select(m => m.MerchantId).ToList();
                var asPublisher = await conversations.GetForPublisherAsync(userId, 0, skip + take, httpContext.RequestAborted);
                var asMerchant = (await conversations.GetForMerchantAsync(merchantIds, 0, skip + take, httpContext.RequestAborted))
                    // 发布人同时是被选商户成员时（自成交理论场景）只按需求方视角展示，避免重复条目
                    .Where(c => c.PublisherUserId != userId)
                    .ToList();

                var merged = asPublisher.Select(c => (Conv: c, Role: "publisher"))
                    .Concat(asMerchant.Select(c => (Conv: c, Role: "merchant")))
                    .OrderByDescending(x => x.Conv.LastMessageAt)
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                var items = new List<object>();
                foreach (var (conv, role) in merged)
                {
                    var demand = await demands.GetByIdAsync(conv.DemandId, httpContext.RequestAborted);
                    var isPublisherRole = role == "publisher";
                    // 对端名称：需求方视角看商家名；商家方视角看发布商户名或个人需求方
                    string counterparty;
                    if (isPublisherRole)
                    {
                        var m = await merchants.GetByIdAsync(conv.MerchantId, httpContext.RequestAborted);
                        counterparty = m?.Name ?? "商家";
                    }
                    else
                    {
                        counterparty = conv.PublisherMerchantId.HasValue && demand != null && !string.IsNullOrWhiteSpace(demand.PublisherMerchantName)
                            ? demand.PublisherMerchantName!
                            : "需求方";
                    }
                    var side = isPublisherRole ? SourcingSenderSide.Publisher : SourcingSenderSide.Merchant;
                    var lastRead = isPublisherRole ? conv.PublisherLastReadAt : conv.MerchantLastReadAt;
                    var unread = await conversations.CountUnreadAsync(conv.Id, side, lastRead, httpContext.RequestAborted);
                    items.Add(new
                    {
                        conversationId = conv.Id,
                        demandId = conv.DemandId,
                        merchantId = conv.MerchantId,
                        role,
                        counterpartyName = counterparty,
                        partNumber = demand?.PartNumber,
                        demandStatus = demand == null ? null : (int?)demand.Status,
                        demandReadOnly = demand == null || demand.Status != SourcingDemandStatus.Published,
                        lastMessageAt = conv.LastMessageAt,
                        lastMessagePreview = conv.LastMessagePreview,
                        unreadCount = unread
                    });
                }

                var totalP = await conversations.CountForPublisherAsync(userId, httpContext.RequestAborted);
                var totalM = await conversations.CountForMerchantAsync(merchantIds, httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new
                {
                    items,
                    total = totalP + totalM
                }, httpContext: httpContext);
            })
            .WithName("GetSourcingConversations")
            .WithSummary("撮合会话列表")
            .WithDescription("需求方与商家方两视角合并，按最后活跃倒序，含未读数与单据状态");

            /// <summary>
            /// 总未读数：全部会话未读之和（首页消息角标数据源）
            /// </summary>
            group.MapGet("/unread-count", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingConversationRepository conversations,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] ISourcingDemandRepository demands,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);
                var userId = currentUser.UserId.Value;
                var merchantIds = (await members.GetActiveByUserIdAsync(userId, httpContext.RequestAborted))
                    .Select(m => m.MerchantId).ToList();

                var total = 0;
                foreach (var conv in await conversations.GetForPublisherAsync(userId, 0, 200, httpContext.RequestAborted))
                    total += await conversations.CountUnreadAsync(conv.Id, SourcingSenderSide.Publisher, conv.PublisherLastReadAt, httpContext.RequestAborted);
                foreach (var conv in await conversations.GetForMerchantAsync(merchantIds, 0, 200, httpContext.RequestAborted))
                {
                    if (conv.PublisherUserId == userId)
                        continue;
                    total += await conversations.CountUnreadAsync(conv.Id, SourcingSenderSide.Merchant, conv.MerchantLastReadAt, httpContext.RequestAborted);
                }
                return ApiResponseHelper.Ok(new { unreadCount = total }, httpContext: httpContext);
            })
            .WithName("GetSourcingConversationUnreadCount")
            .WithSummary("会话总未读数")
            .WithDescription("首页消息图标角标数据源（系统消息未读由通知接口单独返回）");

            /// <summary>
            /// 会话消息（时间正序返回；上滑翻历史用 before 游标）
            /// </summary>
            group.MapGet("/{demandId:guid}/{merchantId:guid}/messages", async (
                Guid demandId,
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingConversationRepository conversations,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] ISourcingResponseRepository responses,
                [FromServices] ISourcingDemandRepository demands,
                HttpContext httpContext,
                [FromQuery] DateTime? before = null,
                [FromQuery] int limit = 30) =>
            {
                var (demand, side) = await ResolveSideAsync(currentUser, demands, members, responses, demandId, merchantId, httpContext.RequestAborted);
                if (side == null)
                    return ApiResponseHelper.NotFound("会话不存在", httpContext: httpContext);

                var conv = await conversations.GetByDemandAndMerchantAsync(demandId, merchantId, httpContext.RequestAborted);
                var take = limit <= 0 ? 30 : Math.Min(limit, MaxMessagePageSize);
                var messages = conv == null
                    ? new List<SourcingMessage>()
                    : await conversations.GetMessagesAsync(conv.Id, before, take, httpContext.RequestAborted);

                // 仓储按倒序取（翻历史游标一致），此处翻转为时间正序供气泡自上而下渲染
                messages.Reverse();
                return ApiResponseHelper.Ok(new
                {
                    conversationId = conv?.Id,
                    demandId,
                    merchantId,
                    role = side == SourcingSenderSide.Publisher ? "publisher" : "merchant",
                    // 单据非进行中时只读（v2.18.0：撮合完成即转线下履约，平台不再承载持续 IM）
                    readOnly = demand == null || demand.Status != SourcingDemandStatus.Published,
                    hasMore = messages.Count == take,
                    messages = messages.Select(m => new
                    {
                        id = m.Id,
                        senderSide = (int)m.SenderSide,
                        mine = m.SenderSide == side,
                        content = m.Content,
                        createdAt = m.CreatedAt
                    })
                }, httpContext: httpContext);
            })
            .WithName("GetSourcingConversationMessages")
            .WithSummary("会话消息")
            .WithDescription("时间正序返回；before 游标翻历史；单据关闭后 readOnly=true");

            /// <summary>
            /// 发送会话消息（会话懒创建；单据非进行中时拒发）
            /// </summary>
            group.MapPost("/{demandId:guid}/{merchantId:guid}/messages", async (
                Guid demandId,
                Guid merchantId,
                [FromBody] SendSourcingMessageRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingConversationRepository conversations,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] ISourcingResponseRepository responses,
                [FromServices] ISourcingDemandRepository demands,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                if (string.IsNullOrWhiteSpace(request?.Content))
                    return ApiResponseHelper.BadRequest("消息内容不能为空", httpContext: httpContext);
                var content = request.Content.Trim();
                if (content.Length > 2000)
                    return ApiResponseHelper.BadRequest("消息内容过长（上限 2000 字）", httpContext: httpContext);

                var (demand, side) = await ResolveSideAsync(currentUser, demands, members, responses, demandId, merchantId, httpContext.RequestAborted);
                if (side == null)
                    return ApiResponseHelper.NotFound("会话不存在", httpContext: httpContext);
                if (demand == null || demand.Status != SourcingDemandStatus.Published)
                    return ApiResponseHelper.BadRequest("该寻货已结束，会话只读", httpContext: httpContext);

                var conv = await conversations.GetByDemandAndMerchantAsync(demandId, merchantId, httpContext.RequestAborted);
                if (conv == null)
                {
                    conv = new SourcingConversation(demandId, merchantId, demand.PublisherUserId, demand.PublisherMerchantId);
                    await conversations.AddAsync(conv, httpContext.RequestAborted);
                }

                var message = new SourcingMessage(conv.Id, currentUser.UserId!.Value, side.Value, content);
                await conversations.AddMessageAsync(message, httpContext.RequestAborted);
                // 发送即视为本方已读到底；同时刷新会话最后活跃与摘要
                conv.MarkRead(side.Value, DateTime.UtcNow);
                conv.Touch(content, message.CreatedAt);
                await conversations.UpdateAsync(conv, httpContext.RequestAborted);
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);

                return ApiResponseHelper.Ok(new
                {
                    id = message.Id,
                    senderSide = (int)message.SenderSide,
                    content = message.Content,
                    createdAt = message.CreatedAt
                }, "已发送", httpContext: httpContext);
            })
            .WithName("SendSourcingMessage")
            .WithSummary("发送会话消息")
            .WithDescription("绑定寻货单×商户的私域消息；会话懒创建，单据关闭后拒发");

            /// <summary>
            /// 标记会话已读（把本方已读水位推到当前）
            /// </summary>
            group.MapPost("/{demandId:guid}/{merchantId:guid}/read", async (
                Guid demandId,
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] ISourcingConversationRepository conversations,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] ISourcingResponseRepository responses,
                [FromServices] ISourcingDemandRepository demands,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var (_, side) = await ResolveSideAsync(currentUser, demands, members, responses, demandId, merchantId, httpContext.RequestAborted);
                if (side == null)
                    return ApiResponseHelper.NotFound("会话不存在", httpContext: httpContext);

                var conv = await conversations.GetByDemandAndMerchantAsync(demandId, merchantId, httpContext.RequestAborted);
                if (conv != null)
                {
                    conv.MarkRead(side.Value, DateTime.UtcNow);
                    await conversations.UpdateAsync(conv, httpContext.RequestAborted);
                    await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                }
                return ApiResponseHelper.Ok("已读", httpContext: httpContext);
            })
            .WithName("MarkSourcingConversationRead")
            .WithSummary("标记会话已读")
            .WithDescription("把当前方的已读水位推至当前时刻");
        }

        /// <summary>
        /// 会话参与方裁决：返回（寻货单, 本方身份）。身份为 null 表示无权访问。
        /// 需求方方=发布人本人或发布商户在职成员；商家方=该商户在职成员且为受邀商户或已应答商户。
        /// </summary>
        private static async Task<(SourcingDemand? Demand, SourcingSenderSide? Side)> ResolveSideAsync(
            ICurrentUserService currentUser,
            ISourcingDemandRepository demands,
            IMerchantMemberRepository members,
            ISourcingResponseRepository responses,
            Guid demandId,
            Guid merchantId,
            CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                return (null, null);
            var userId = currentUser.UserId.Value;

            var demand = await demands.GetByIdAsync(demandId, cancellationToken);
            if (demand == null)
                return (null, null);

            // 需求方方：发布人本人
            if (demand.PublisherUserId == userId)
                return (demand, SourcingSenderSide.Publisher);
            // 需求方方：商户名义发布时，发布商户的在职成员同权
            if (demand.PublisherMerchantId.HasValue)
            {
                var pubMembership = await members.GetActiveByUserAndMerchantAsync(userId, demand.PublisherMerchantId.Value, cancellationToken);
                if (pubMembership != null)
                    return (demand, SourcingSenderSide.Publisher);
            }

            // 商家方：须为该商户在职成员，且是该单的受邀商户或已应答商户（防任意商户开聊骚扰）
            var membership = await members.GetActiveByUserAndMerchantAsync(userId, merchantId, cancellationToken);
            if (membership != null)
            {
                if (demand.InvitedMerchantIds.Contains(merchantId))
                    return (demand, SourcingSenderSide.Merchant);
                var myResponse = await responses.GetByDemandAndMerchantAsync(demandId, merchantId, cancellationToken);
                if (myResponse != null)
                    return (demand, SourcingSenderSide.Merchant);
            }

            return (demand, null);
        }
    }

    /// <summary>发送会话消息请求体</summary>
    /// <param name="Content">消息正文（纯文本，1-2000 字）</param>
    public record SendSourcingMessageRequest(string Content);
}
