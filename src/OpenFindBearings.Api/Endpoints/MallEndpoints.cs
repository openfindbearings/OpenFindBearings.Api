using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 商城接口（v2.3.0 商城虚拟权益）：目录浏览/积分兑换/我的订单 + Admin 目录管理。
    /// 一期实装置顶卡履约；兑换走"全量校验→扣分→履约"，履约异常自动退分
    /// </summary>
    public static class MallEndpoints
    {
        /// <summary>
        /// 映射商城端点
        /// </summary>
        public static void MapMallEndpoints(this IEndpointRouteBuilder app)
        {
            // ============ 用户端（登录用户） ============
            var group = app.MapGroup("/api/mall").RequireAuthorization();

            /// <summary>
            /// 商城目录：生效价（闪购窗口内自动取闪购价）+ 库存态 + 当前余额（三态按钮依据）
            /// </summary>
            group.MapGet("/items", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var catalog = await mallService.GetCatalogAsync(currentUser.UserId.Value);
                return ApiResponseHelper.Ok(new
                {
                    items = catalog.Items.Select(i => new
                    {
                        id = i.Id,
                        key = i.Key,
                        name = i.Name,
                        description = i.Description,
                        icon = i.Icon,
                        category = i.Category,
                        price = i.Price,
                        originalPrice = i.OriginalPrice,
                        flashing = i.Flashing,
                        flashEnd = i.FlashEnd,
                        durationHours = i.DurationHours,
                        stock = i.Stock,
                        soldCount = i.SoldCount,
                        soldOut = i.SoldOut,
                        // v2.4.0 挂礼："来自 XX 商家"（平台权益为 null）
                        ownerMerchantName = i.OwnerMerchantName
                    }),
                    balance = catalog.Balance
                }, httpContext: httpContext);
            })
            .WithName("GetMallItems")
            .WithSummary("商城目录")
            .WithDescription("上架虚拟权益列表（含闪购价/库存态）与当前用户积分余额");

            /// <summary>
            /// 积分兑换（置顶卡需带 targetRef=MerchantBearingId；requestId 为客户端幂等键）
            /// </summary>
            group.MapPost("/redeem", async (
                [FromBody] RedeemRequest req,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var result = await mallService.RedeemAsync(currentUser.UserId.Value, req.ItemId, req.TargetRef, req.RequestId, req.UseTreasury);
                if (!result.Success)
                    return ApiResponseHelper.BadRequest(result.Message ?? "兑换失败", httpContext: httpContext);

                return ApiResponseHelper.Ok(new
                {
                    orderId = result.OrderId,
                    pointsSpent = result.PointsSpent,
                    pinnedUntil = result.PinnedUntil
                }, "兑换成功", httpContext);
            })
            .WithName("RedeemMallItem")
            .WithSummary("积分兑换")
            .WithDescription("扣积分并自动履约虚拟权益；余额不足/越权/非在售均在扣分前拦截");

            /// <summary>
            /// 实物礼品兑换（v2.4.0）：收货三件套必填，托管扣分，自兑排除+同址同机月限单前置拦截
            /// </summary>
            group.MapPost("/redeem-gift", async (
                [FromBody] RedeemGiftRequest req,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var result = await mallService.RedeemGiftAsync(currentUser.UserId.Value, req.ItemId,
                    req.ReceiverName, req.ReceiverPhone, req.ReceiverAddress, req.RequestId);
                if (!result.Success)
                    return ApiResponseHelper.BadRequest(result.Message ?? "兑换失败", httpContext: httpContext);

                return ApiResponseHelper.Ok(new { orderId = result.OrderId, pointsSpent = result.PointsSpent },
                    "兑换成功，等待商家发货", httpContext);
            })
            .WithName("RedeemMallGift")
            .WithSummary("礼品兑换")
            .WithDescription("实物礼品托管兑换：确认收货或发货 7 天后结算进发布商户金库");

            /// <summary>
            /// 确认收货（买家本人）：结算入商家金库（幂等，月顶超额部分留平台）
            /// </summary>
            group.MapPost("/orders/{id:guid}/confirm-receipt", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var (ok, message, settled) = await mallService.ConfirmReceiptAsync(currentUser.UserId.Value, id);
                return ok
                    ? ApiResponseHelper.Ok(new { settled }, settled > 0 ? "已确认收货，积分已结算给商家" : "已确认收货", httpContext)
                    : ApiResponseHelper.BadRequest(message ?? "操作失败", httpContext: httpContext);
            })
            .WithName("ConfirmMallReceipt")
            .WithSummary("确认收货");

            /// <summary>
            /// 我的兑换订单（时间倒序分页）
            /// </summary>
            group.MapGet("/orders", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var p = page <= 0 ? 1 : page;
                var ps = pageSize <= 0 || pageSize > 50 ? 20 : pageSize;
                var (items, total) = await mallService.GetMyOrdersAsync(currentUser.UserId.Value, p, ps);
                return ApiResponseHelper.Paged(items.Select(o => new
                {
                    id = o.Id,
                    itemKey = o.ItemKey,
                    itemName = o.ItemName,
                    pointsSpent = o.PointsSpent,
                    status = o.Status,
                    remark = o.Remark,
                    createdAt = o.CreatedAt,
                    fulfilledAt = o.FulfilledAt,
                    // v2.4.0 实物礼品物流态（虚拟权益恒 0）
                    shipStatus = o.ShipStatus,
                    shipTracking = o.ShipTracking,
                    shippedAt = o.ShippedAt,
                    receivedAt = o.ReceivedAt
                }).ToList(), total, p, ps, httpContext);
            })
            .WithName("GetMallOrders")
            .WithSummary("我的兑换订单");

            // ============ Admin 端（目录管理，复用 system.manage 免新增权限迁移） ============
            var admin = app.MapGroup("/api/admin/mall").RequireAuthorization();

            /// <summary>商品目录全量（含停用）</summary>
            admin.MapGet("/items", async (
                [FromServices] IMallItemRepository repo,
                HttpContext httpContext) =>
            {
                var items = await repo.GetAllAsync();
                return ApiResponseHelper.Ok(items.Select(i => new
                {
                    id = i.Id,
                    key = i.Key,
                    name = i.Name,
                    description = i.Description,
                    icon = i.Icon,
                    category = (int)i.Category,
                    pointPrice = i.PointPrice,
                    flashPrice = i.FlashPrice,
                    flashStart = i.FlashStart,
                    flashEnd = i.FlashEnd,
                    durationHours = i.DurationHours,
                    stock = i.Stock,
                    soldCount = i.SoldCount,
                    enabled = i.Enabled,
                    sortOrder = i.SortOrder
                }), httpContext: httpContext);
            })
            .WithName("AdminGetMallItems")
            .WithSummary("商城目录全量")
            .RequirePermission("system.manage");

            /// <summary>编辑商品（价格/闪购窗口/库存/时长/上下架/文案/排序；Key 与类别不可改）</summary>
            admin.MapPut("/items/{id:guid}", async (
                Guid id,
                [FromBody] UpdateMallItemRequest req,
                [FromServices] IMallItemRepository repo,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var item = await repo.GetByIdAsync(id);
                if (item == null)
                    return ApiResponseHelper.NotFound("商品不存在", httpContext);

                item.Update(req.Name, req.Description, req.Icon, req.PointPrice,
                    req.FlashPrice, req.FlashStart, req.FlashEnd,
                    req.DurationHours, req.Stock, req.Enabled, req.SortOrder);
                await repo.UpdateAsync(item);
                // 端点直连仓储不走 MediatR 管道，必须显式提交（与积分规则 PUT 同模式）
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { id = item.Id }, "已保存", httpContext);
            })
            .WithName("AdminUpdateMallItem")
            .WithSummary("编辑商城商品")
            .RequirePermission("system.manage");

            // ===== v2.4.0 商家挂礼审核定档 + 争议退款（审核线复用 merchant.verify，交易处置复用 merchant.manage） =====

            /// <summary>待审礼品队列（含归属商户名供审核参考）</summary>
            admin.MapGet("/gifts/pending", async (
                [FromServices] IMallItemRepository repo,
                [FromServices] IMerchantRepository merchants,
                HttpContext httpContext) =>
            {
                var items = await repo.GetPendingGiftsAsync();
                var ownerNames = new Dictionary<Guid, string?>();
                foreach (var ownerId in items.Select(i => i.OwnerMerchantId!.Value).Distinct())
                    ownerNames[ownerId] = (await merchants.GetByIdAsync(ownerId))?.Name;
                return ApiResponseHelper.Ok(items.Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    description = i.Description,
                    imageKey = i.Icon,
                    stock = i.Stock,
                    ownerMerchantId = i.OwnerMerchantId,
                    ownerMerchantName = i.OwnerMerchantId.HasValue && ownerNames.TryGetValue(i.OwnerMerchantId.Value, out var n) ? n : null,
                    createdAt = i.CreatedAt
                }), httpContext: httpContext);
            })
            .WithName("AdminGetPendingGifts")
            .WithSummary("待审礼品")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 礼品审核定档：平台敲定积分价（单价封顶防定向转移）并放行上架
            /// </summary>
            admin.MapPost("/gifts/{id:guid}/approve", async (
                Guid id,
                [FromBody] ApproveGiftRequest req,
                [FromServices] IMallItemRepository repo,
                [FromServices] OpenFindBearings.Domain.Repositories.ISystemConfigRepository configs,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var item = await repo.GetByIdAsync(id);
                if (item == null || !item.IsMerchantGift)
                    return ApiResponseHelper.NotFound("礼品不存在", httpContext);
                if (item.AuditState != 1)
                    return ApiResponseHelper.BadRequest("该礼品不在待审状态", httpContext: httpContext);

                var ceiling = await configs.GetValueAsync<int?>("Business.GiftPriceCeiling", null) ?? 3000;
                if (req.PointPrice <= 0 || req.PointPrice > ceiling)
                    return ApiResponseHelper.BadRequest($"积分价须在 1-{ceiling} 之间（单价封顶防定向转移）", httpContext: httpContext);

                item.PassGiftAudit(req.PointPrice, null);
                await repo.UpdateAsync(item);
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(message: "已通过并定档", httpContext: httpContext);
            })
            .WithName("AdminApproveMallGift")
            .WithSummary("礼品审核通过")
            .RequirePermission("merchant.verify");

            /// <summary>礼品审核驳回（原因商户可见）</summary>
            admin.MapPost("/gifts/{id:guid}/reject", async (
                Guid id,
                [FromBody] RejectRequest req,
                [FromServices] IMallItemRepository repo,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var item = await repo.GetByIdAsync(id);
                if (item == null || !item.IsMerchantGift)
                    return ApiResponseHelper.NotFound("礼品不存在", httpContext);
                if (item.AuditState != 1)
                    return ApiResponseHelper.BadRequest("该礼品不在待审状态", httpContext: httpContext);

                item.RejectGiftAudit(string.IsNullOrWhiteSpace(req.Reason) ? "不符合平台挂礼要求" : req.Reason.Trim());
                await repo.UpdateAsync(item);
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(message: "已驳回", httpContext: httpContext);
            })
            .WithName("AdminRejectMallGift")
            .WithSummary("礼品审核驳回")
            .RequirePermission("merchant.verify");

            /// <summary>托管中实物礼品单（待发货/已发货未收货，争议退款队列）</summary>
            admin.MapGet("/orders/escrow", async (
                [FromServices] IMallOrderRepository orders,
                HttpContext httpContext) =>
            {
                var list = await orders.GetEscrowGiftOrdersAsync(100);
                return ApiResponseHelper.Ok(list.Select(o => new
                {
                    id = o.Id,
                    itemName = o.ItemName,
                    pointsSpent = o.PointsSpent,
                    shipStatus = o.ShipStatus,
                    receiverName = o.ReceiverName,
                    receiverPhone = o.ReceiverPhone,
                    createdAt = o.CreatedAt,
                    shippedAt = o.ShippedAt
                }), httpContext: httpContext);
            })
            .WithName("AdminGetMallEscrowOrders")
            .WithSummary("托管礼品单")
            .RequirePermission("merchant.manage");

            /// <summary>争议退款（仅未结算实物单原路退分；已收货单不可反冲）</summary>
            admin.MapPost("/orders/{id:guid}/refund", async (
                Guid id,
                [FromBody] RejectRequest req,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                var (ok, message) = await mallService.RefundDisputeAsync(id, req.Reason ?? "");
                return ok
                    ? ApiResponseHelper.Ok(message: "已退款", httpContext: httpContext)
                    : ApiResponseHelper.BadRequest(message ?? "退款失败", httpContext: httpContext);
            })
            .WithName("AdminRefundMallOrder")
            .WithSummary("礼品订单争议退款")
            .RequirePermission("merchant.manage");
        }
    }

    /// <summary>兑换请求体（useTreasury=true 改从目标商品所属商户金库支出，仅管理员）</summary>
    public record RedeemRequest(Guid ItemId, Guid? TargetRef, string? RequestId, bool UseTreasury = false);

    /// <summary>实物礼品兑换请求体（收货三件套必填，v2.4.0 托管扣款）</summary>
    public record RedeemGiftRequest(Guid ItemId, string ReceiverName, string ReceiverPhone,
        string ReceiverAddress, string? RequestId);

    /// <summary>Admin 礼品定档请求体</summary>
    public record ApproveGiftRequest(int PointPrice);

    /// <summary>通用原因请求体（驳回/争议退款）</summary>
    public record RejectRequest(string? Reason);

    /// <summary>Admin 编辑商品请求体</summary>
    public record UpdateMallItemRequest(
        string Name, string Description, string Icon, int PointPrice,
        int? FlashPrice, DateTime? FlashStart, DateTime? FlashEnd,
        int? DurationHours, int Stock, bool Enabled, int SortOrder);
}
