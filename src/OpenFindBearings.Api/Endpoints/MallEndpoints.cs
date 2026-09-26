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
                        soldOut = i.SoldOut
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

                var result = await mallService.RedeemAsync(currentUser.UserId.Value, req.ItemId, req.TargetRef, req.RequestId);
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
                    fulfilledAt = o.FulfilledAt
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
        }
    }

    /// <summary>兑换请求体</summary>
    public record RedeemRequest(Guid ItemId, Guid? TargetRef, string? RequestId);

    /// <summary>Admin 编辑商品请求体</summary>
    public record UpdateMallItemRequest(
        string Name, string Description, string Icon, int PointPrice,
        int? FlashPrice, DateTime? FlashStart, DateTime? FlashEnd,
        int? DurationHours, int Stock, bool Enabled, int SortOrder);
}
