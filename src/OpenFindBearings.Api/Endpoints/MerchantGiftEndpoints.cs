using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 商家礼品挂售与金库端点（v2.4.0 商家经济）：
    /// 挂礼申请/我的礼品/下架/礼品图上传 + 金库余额/流水 + 礼品订单查询/发货。
    /// 鉴权全部走业务资格（在职成员 + IsAdmin），无 RBAC 权限键——app 端能力靠登录态与成员表（定案）
    /// </summary>
    public static class MerchantGiftEndpoints
    {
        /// <summary>
        /// 映射商家礼品/金库端点
        /// </summary>
        public static void MapMerchantGiftEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/merchant").RequireAuthorization();

            /// <summary>
            /// 申请挂礼（仅商户管理员）：进待审态，价格由平台审核定档
            /// </summary>
            group.MapPost("/gifts", async (
                [FromBody] CreateGiftRequest req,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallItemRepository items,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Description) || req.Stock <= 0)
                    return ApiResponseHelper.BadRequest("名称/描述必填，数量须为正", httpContext: httpContext);
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null || !member.IsAdmin)
                    return ApiResponseHelper.Forbidden("仅商户管理员可申请挂礼", httpContext);

                // Key 全局唯一：商户短码+时间戳，秒级冲突概率可忽略（唯一索引兜底）
                var shortCode = merchantId.Value.ToString("N")[..8]; var key = $"gift:{shortCode}:{DateTime.UtcNow:yyyyMMddHHmmss}";
                var item = MallItem.CreateGift(key, merchantId.Value, req.Name.Trim(), req.Description.Trim(),
                    req.ImageKey?.Trim() ?? "", req.Stock);
                await items.AddAsync(item);
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { id = item.Id }, "已提交，等待平台审核定档", httpContext);
            })
            .WithName("CreateMerchantGift")
            .WithSummary("申请挂礼");

            /// <summary>
            /// 我的挂礼列表（含各审核态；管理员可见，员工不可见管理面）
            /// </summary>
            group.MapGet("/gifts", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallItemRepository items,
                [FromServices] IMerchantMemberRepository members,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null || !member.IsAdmin)
                    return ApiResponseHelper.Forbidden("仅商户管理员可查看挂礼管理", httpContext);

                var list = await items.GetByOwnerAsync(merchantId.Value);
                return ApiResponseHelper.Ok(list.Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    description = g.Description,
                    imageKey = g.Icon,
                    pointPrice = g.PointPrice,
                    stock = g.Stock,
                    soldCount = g.SoldCount,
                    auditState = g.AuditState,
                    auditRemark = g.AuditRemark,
                    enabled = g.Enabled,
                    createdAt = g.CreatedAt
                }), httpContext: httpContext);
            })
            .WithName("GetMerchantGifts")
            .WithSummary("我的挂礼列表");

            /// <summary>
            /// 下架自己的礼品（存量订单不受影响，停止新兑换）
            /// </summary>
            group.MapPost("/gifts/{id:guid}/offshelf", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallItemRepository items,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null || !member.IsAdmin)
                    return ApiResponseHelper.Forbidden("仅商户管理员可下架礼品", httpContext);
                var item = await items.GetByIdAsync(id);
                if (item == null || item.OwnerMerchantId != merchantId.Value)
                    return ApiResponseHelper.NotFound("礼品不存在", httpContext);

                item.TakeOffShelfByOwner();
                await items.UpdateAsync(item);
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(message: "已下架", httpContext: httpContext);
            })
            .WithName("OffShelfMerchantGift")
            .WithSummary("下架礼品");

            /// <summary>
            /// 礼品图上传（存对象存储返回相对 URL，与证照上传同机制；仅图片、5MB 上限）
            /// </summary>
            group.MapPost("/gifts/image", async (
                IFormFile file,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] IObjectStorageService storage,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null || !member.IsAdmin)
                    return ApiResponseHelper.Forbidden("仅商户管理员可上传礼品图", httpContext);
                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请选择图片文件", httpContext: httpContext);
                if (file.Length > 5 * 1024 * 1024)
                    return ApiResponseHelper.BadRequest("图片不能超过 5MB", httpContext: httpContext);

                var ext = FileUploadHelper.GetSafeExtension(file);
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                    return ApiResponseHelper.BadRequest("仅支持 JPG、PNG、WEBP 图片", httpContext: httpContext);

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, httpContext.RequestAborted);
                var key = $"uploads/mall-gifts/{Guid.NewGuid():N}{ext}";
                var url = await storage.UploadAsync(key, ms.ToArray(), FileUploadHelper.ContentTypeFromExtension(ext), httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { url }, httpContext: httpContext);
            })
            .WithName("UploadMallGiftImage")
            .WithSummary("礼品图上传")
            .DisableAntiforgery();

            /// <summary>
            /// 金库账户（仅商户管理员）：余额+累计；未开户回零
            /// </summary>
            group.MapGet("/treasury", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantPointsService treasury,
                [FromServices] IMerchantMemberRepository members,
                [FromServices] IMerchantRepository merchants,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null || !member.IsAdmin)
                    return ApiResponseHelper.Forbidden("仅商户管理员可查看金库", httpContext);

                var account = await treasury.GetAccountAsync(merchantId.Value);
                // v2.5.0 商家经济：金库页头部展示商家等级（入驻/认证/口碑/金牌，v2.13.0 Lv3 改名口碑）
                var merchant = await merchants.GetByIdAsync(merchantId.Value);
                return ApiResponseHelper.Ok(new
                {
                    balance = account?.Balance ?? 0,
                    totalEarned = account?.TotalEarned ?? 0,
                    totalSpent = account?.TotalSpent ?? 0,
                    grade = (int)(merchant?.Grade ?? Domain.Enums.MerchantGrade.Unknown),
                    gradeDisplay = merchant?.GetGradeDisplayName() ?? "未定级"
                }, httpContext: httpContext);
            })
            .WithName("GetMerchantTreasury")
            .WithSummary("金库余额");

            /// <summary>
            /// 金库流水分页（仅管理员；trickle/结算/消费/燃烧四类可读文案）
            /// </summary>
            group.MapGet("/treasury/transactions", async (
                int page,
                int pageSize,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantPointsService treasury,
                [FromServices] IMerchantMemberRepository members,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null || !member.IsAdmin)
                    return ApiResponseHelper.Forbidden("仅商户管理员可查看金库流水", httpContext);

                var p = page <= 0 ? 1 : page;
                var ps = pageSize <= 0 || pageSize > 50 ? 20 : pageSize;
                var (items, total) = await treasury.GetTransactionsAsync(merchantId.Value, p, ps);
                return ApiResponseHelper.Paged(items.Select(t => new
                {
                    direction = t.Direction,
                    scene = t.GrantType,
                    amount = t.Amount,
                    balanceAfter = t.BalanceAfter,
                    remark = t.Remark,
                    createdAt = t.CreatedAt
                }).ToList(), total, p, ps, httpContext);
            })
            .WithName("GetMerchantTreasuryTransactions")
            .WithSummary("金库流水");

            /// <summary>
            /// 礼品订单列表（该商户挂礼的兑换单，含收货信息供发货；在职成员皆可看）
            /// </summary>
            group.MapGet("/gift-orders", async (
                int? shipStatus,
                int page,
                int pageSize,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallOrderRepository orders,
                [FromServices] IMerchantMemberRepository members,
                HttpContext httpContext) =>
            {
                var merchantId = RequireCurrentMerchant(currentUser, httpContext, out var fail);
                if (merchantId == null) return fail!;
                var member = await members.GetActiveByUserAndMerchantAsync(currentUser.UserId!.Value, merchantId.Value);
                if (member == null)
                    return ApiResponseHelper.Forbidden("非该商户在职成员", httpContext);

                var p = page <= 0 ? 1 : page;
                var ps = pageSize <= 0 || pageSize > 50 ? 20 : pageSize;
                var (items, total) = await orders.GetByOwnerMerchantPagedAsync(merchantId.Value, shipStatus, p, ps);
                return ApiResponseHelper.Paged(items.Select(o => new
                {
                    id = o.Id,
                    itemName = o.ItemName,
                    pointsSpent = o.PointsSpent,
                    shipStatus = o.ShipStatus,
                    receiverName = o.ReceiverName,
                    receiverPhone = o.ReceiverPhone,
                    receiverAddress = o.ReceiverAddress,
                    shipTracking = o.ShipTracking,
                    shippedAt = o.ShippedAt,
                    receivedAt = o.ReceivedAt,
                    createdAt = o.CreatedAt
                }).ToList(), total, p, ps, httpContext);
            })
            .WithName("GetMerchantGiftOrders")
            .WithSummary("礼品订单列表");

            /// <summary>
            /// 发货登记（在职成员可操作，员工也可发；单号必填）
            /// </summary>
            group.MapPost("/gift-orders/{id:guid}/ship", async (
                Guid id,
                [FromBody] ShipGiftRequest req,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMallService mallService,
                HttpContext httpContext) =>
            {
                var (ok, message) = await mallService.ShipGiftOrderAsync(currentUser.UserId!.Value, id, req.Tracking ?? "", httpContext.RequestAborted);
                return ok ? ApiResponseHelper.Ok(message: "已发货", httpContext: httpContext) : ApiResponseHelper.BadRequest(message ?? "发货失败", httpContext: httpContext);
            })
            .WithName("ShipMerchantGiftOrder")
            .WithSummary("礼品发货");
        }

        /// <summary>当前商户上下文校验（X-Merchant-Id 已由中间件解析进 CurrentMerchantId）；不通过时输出 400</summary>
        private static Guid? RequireCurrentMerchant(ICurrentUserService currentUser, HttpContext httpContext, out IResult? fail)
        {
            if (!currentUser.UserId.HasValue || !currentUser.CurrentMerchantId.HasValue)
            {
                fail = ApiResponseHelper.BadRequest("请先选择当前商户", httpContext: httpContext);
                return null;
            }
            fail = null;
            return currentUser.CurrentMerchantId.Value;
        }
    }

    /// <summary>挂礼申请请求体</summary>
    public record CreateGiftRequest(string Name, string Description, string? ImageKey, int Stock);

    /// <summary>发货请求体</summary>
    public record ShipGiftRequest(string? Tracking);
}
