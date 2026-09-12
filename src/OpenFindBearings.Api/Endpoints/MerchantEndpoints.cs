using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.MerchantBearings.Commands;
using OpenFindBearings.Application.Commands.MerchantBearings.PutOnShelf;
using OpenFindBearings.Application.Commands.MerchantBearings.SetPriceVisibility;
using OpenFindBearings.Application.Commands.MerchantBearings.TakeOffShelf;
using OpenFindBearings.Application.Commands.MerchantBearings.UpdateMerchantBearing;
using OpenFindBearings.Application.Commands.Merchants.ActivateMerchantMember;
using OpenFindBearings.Application.Commands.Merchants.AddStaff;
using OpenFindBearings.Application.Commands.Merchants.ChangeMerchantMemberRole;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Application.Commands.Merchants.RemoveStaff;
using OpenFindBearings.Application.Commands.Merchants.SubmitLicense;
using OpenFindBearings.Application.Commands.Merchants.SuspendMerchantMember;
using OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByMerchant;
using OpenFindBearings.Application.Queries.Merchants.GetMerchant;
using OpenFindBearings.Application.Queries.Merchants.GetMerchantByUserId;
using OpenFindBearings.Application.Queries.Merchants.GetMerchantStaff;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 商家管理接口（需商家角色）
    /// </summary>
    public static class MerchantEndpoints
    {
        public static void MapMerchantEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/merchant")
                .WithTags("商家管理接口")
                .RequireAuthorization("Merchant");

            // ============ 3.1 基础管理 ============

            /// <summary>
            /// 获取店铺信息
            /// </summary>
            group.MapGet("/profile", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMerchantByUserIdQuery
                {
                    UserId = currentUser.UserId.Value
                };
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound("未找到所属商家", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMerchantProfile")
            .WithSummary("获取店铺信息")
            .WithDescription("获取当前登录商家用户的店铺详细信息");

            /// <summary>
            /// 更新店铺信息
            /// </summary>
            group.MapPut("/profile", async (
                UpdateMerchantCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var merchant = await GetCurrentMerchantAsync(currentUser, mediator);

                if (merchant == null)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var updateCommand = command with { Id = merchant.Id };
                await mediator.Send(updateCommand);

                return ApiResponseHelper.Ok("店铺信息更新成功", httpContext);
            })
            .WithName("UpdateMerchantProfile")
            .WithSummary("更新店铺信息")
            .WithDescription("更新当前商家用户的店铺基本信息");

            /// <summary>
            /// 上传营业执照
            /// </summary>
            group.MapPost("/license", async (
            IFormFile file,
            [FromServices] ICurrentUserService currentUser,
            [FromServices] IMediator mediator,
            [FromServices] IWebHostEnvironment environment,
            HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请上传文件", httpContext: httpContext);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return ApiResponseHelper.BadRequest("只支持 JPG、PNG、PDF 格式", httpContext: httpContext);

                if (file.Length > 5 * 1024 * 1024)
                    return ApiResponseHelper.BadRequest("文件大小不能超过5MB", httpContext: httpContext);

                try
                {
                    var merchantQuery = new GetMerchantByUserIdQuery
                    {
                        UserId = currentUser.UserId.Value
                    };
                    var merchant = await mediator.Send(merchantQuery);

                    if (merchant == null)
                        return ApiResponseHelper.NotFound("未找到所属商家", httpContext: httpContext);

                    var uploadsFolder = Path.Combine(environment.WebRootPath, "uploads", "licenses");
                    Directory.CreateDirectory(uploadsFolder);

                    // 时区规范修复：文件名时间戳统一 UTC（原 DateTime.Now 依赖服务器时区）
                    var fileName = $"{merchant.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileUrl = $"/uploads/licenses/{fileName}";

                    var licenseCommand = new SubmitLicenseCommand
                    {
                        MerchantId = merchant.Id,
                        LicenseUrl = fileUrl,
                        SubmittedBy = currentUser.UserId.Value
                    };
                    var verificationId = await mediator.Send(licenseCommand);

                    return ApiResponseHelper.Ok(new
                    {
                        verificationId,
                        url = fileUrl,
                        message = "营业执照已上传，等待审核"
                    }, httpContext: httpContext);
                }
                catch (Exception ex)
                {
                    return ApiResponseHelper.Problem(
                        title: "上传失败",
                        detail: ex.Message,
                        httpContext: httpContext
                    );
                }
            })
            .WithName("UploadLicense")
            .WithSummary("上传营业执照")
            .WithDescription("上传营业执照用于商家认证")
            .DisableAntiforgery();

            /// <summary>
            /// 获取员工列表
            /// </summary>
            group.MapGet("/staff", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var merchant = await GetCurrentMerchantAsync(currentUser, mediator);

                if (merchant == null)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var staffList = await mediator.Send(new GetMerchantStaffQuery(merchant.Id));

                var totalCount = staffList.Count();
                var pagedItems = staffList
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return ApiResponseHelper.Paged(
                    pagedItems,
                    totalCount,
                    page,
                    pageSize,
                    httpContext
                );
            })
            .WithName("GetStaffList")
            .WithSummary("获取员工列表")
            .WithDescription("获取当前商家的所有员工列表");

            /// <summary>
            /// 添加员工
            /// </summary>
            group.MapPost("/staff", async (
                AddStaffCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPermissionService permissionService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var merchant = await GetCurrentMerchantAsync(currentUser, mediator);

                if (merchant == null)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                {
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);
                }

                var addCommand = command with
                {
                    MerchantId = merchant.Id,
                    OperatorId = currentUser.UserId.Value
                };

                var result = await mediator.Send(addCommand);
                return ApiResponseHelper.Ok(new { id = result, message = "员工添加成功" }, httpContext: httpContext);
            })
            .WithName("AddStaff")
            .WithSummary("添加员工")
            .WithDescription("添加新员工到当前商家（需商家管理员权限）");

            /// <summary>
            /// 移除员工
            /// </summary>
            group.MapDelete("/staff/{userId:guid}", async (
                Guid userId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPermissionService permissionService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                {
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);
                }

                var command = new RemoveStaffCommand
                {
                    UserId = userId,
                    OperatorId = currentUser.UserId.Value,
                    MerchantId = currentUser.CurrentMerchantId ?? Guid.Empty
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("员工移除成功", httpContext);
            })
            .WithName("RemoveStaff")
            .WithSummary("移除员工")
            .WithDescription("从当前商家移除员工（需商家管理员权限）");

            /// <summary>
            /// 停用成员（管理员离职/异常处置，可恢复）
            /// </summary>
            group.MapPost("/members/{userId:guid}/suspend", async (
                Guid userId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPermissionService permissionService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                {
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);
                }

                if (!currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var command = new SuspendMerchantMemberCommand
                {
                    UserId = userId,
                    MerchantId = currentUser.CurrentMerchantId.Value,
                    OperatorId = currentUser.UserId.Value
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("成员已停用", httpContext);
            })
            .WithName("SuspendMerchantMember")
            .WithSummary("停用成员")
            .WithDescription("停用商户成员，立即失去操作权限，可恢复（需商家管理员权限）");

            /// <summary>
            /// 恢复被停用的成员
            /// </summary>
            group.MapPost("/members/{userId:guid}/activate", async (
                Guid userId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPermissionService permissionService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                {
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);
                }

                if (!currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var command = new ActivateMerchantMemberCommand
                {
                    UserId = userId,
                    MerchantId = currentUser.CurrentMerchantId.Value,
                    OperatorId = currentUser.UserId.Value
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("成员已恢复", httpContext);
            })
            .WithName("ActivateMerchantMember")
            .WithSummary("恢复成员")
            .WithDescription("恢复被停用的商户成员（需商家管理员权限）");

            /// <summary>
            /// 变更成员角色（管理员/员工）
            /// </summary>
            group.MapPut("/members/{userId:guid}/role", async (
                Guid userId,
                ChangeMerchantMemberRoleRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPermissionService permissionService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                {
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);
                }

                if (!currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var command = new ChangeMerchantMemberRoleCommand
                {
                    UserId = userId,
                    MerchantId = currentUser.CurrentMerchantId.Value,
                    Role = request.Role,
                    OperatorId = currentUser.UserId.Value
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("成员角色已变更", httpContext);
            })
            .WithName("ChangeMerchantMemberRole")
            .WithSummary("变更成员角色")
            .WithDescription("变更商户成员角色（需商家管理员权限）");

            // ============ 3.2 产品管理 ============

            /// <summary>
            /// 获取自家轴承列表
            /// </summary>
            group.MapGet("/bearings", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] bool? onlyOnSale = null,
                [FromQuery] bool? pendingOnly = null) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var merchant = await GetCurrentMerchantAsync(currentUser, mediator);

                if (merchant == null)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var query = new GetMerchantBearingsByMerchantQuery
                {
                    MerchantId = merchant.Id,
                    OnlyOnSale = onlyOnSale,
                    PendingOnly = pendingOnly,
                    Page = page,
                    PageSize = pageSize,
                    IsAuthenticated = true
                };

                var result = await mediator.Send(query);

                return ApiResponseHelper.Paged(
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("GetMyMerchantBearings")
            .WithSummary("获取自家轴承列表")
            .WithDescription("获取当前商家店铺的所有轴承产品");

            /// <summary>
            /// 添加轴承到店铺
            /// </summary>
            group.MapPost("/bearings", async (
                CreateMerchantBearingCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var merchant = await GetCurrentMerchantAsync(currentUser, mediator);

                if (merchant == null)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var createCommand = command with
                {
                    MerchantId = merchant.Id
                };

                var id = await mediator.Send(createCommand);
                return ApiResponseHelper.Ok(new { id, message = "添加成功，等待审核" }, httpContext: httpContext);
            })
            .WithName("CreateMerchantBearing")
            .WithSummary("添加轴承到店铺")
            .WithDescription("添加新的轴承产品到店铺（需要审核）");

            /// <summary>
            /// 更新店铺轴承
            /// </summary>
            group.MapPut("/bearings/{id:guid}", async (
                Guid id,
                UpdateMerchantBearingCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var updateCommand = command with { Id = id, UserId = currentUser.UserId.Value };
                await mediator.Send(updateCommand);

                return ApiResponseHelper.Ok("更新成功，等待审核", httpContext);
            })
            .WithName("UpdateMerchantBearing")
            .WithSummary("更新店铺轴承")
            .WithDescription("更新店铺中轴承的信息（需要审核）");

            /// <summary>
            /// 设置价格可见性
            /// </summary>
            group.MapPut("/bearings/{id:guid}/price-visibility", async (
                Guid id,
                SetPriceVisibilityCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var updateCommand = command with { MerchantBearingId = id, UserId = currentUser.UserId.Value };
                await mediator.Send(updateCommand);

                return ApiResponseHelper.Ok("价格可见性设置成功", httpContext);
            })
            .WithName("SetPriceVisibility")
            .WithSummary("设置价格可见性")
            .WithDescription("设置产品价格的可见性（Public/LoginRequired）");

            /// <summary>
            /// 上架轴承
            /// </summary>
            group.MapPost("/bearings/{id:guid}/onshelf", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new PutOnShelfCommand
                {
                    MerchantBearingId = id,
                    UserId = currentUser.UserId.Value
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("上架成功", httpContext);
            })
            .WithName("PutOnShelf")
            .WithSummary("上架轴承")
            .WithDescription("上架自家轴承产品");

            /// <summary>
            /// 下架轴承
            /// </summary>
            group.MapPost("/bearings/{id:guid}/offshelf", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new TakeOffShelfCommand
                {
                    MerchantBearingId = id,
                    UserId = currentUser.UserId.Value
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("下架成功", httpContext);
            })
            .WithName("TakeOffShelf")
            .WithSummary("下架轴承")
            .WithDescription("下架自家轴承产品");

            /// <summary>
            /// Excel 批量导入在售商品（仅商户管理员）
            /// 解析能力复用 Sync /api/inventory/import，写库 DataSourceType=Manual（不被爬虫覆盖）
            /// </summary>
            group.MapPost("/inventory/import", async (
                IFormFile file,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IPermissionService permissionService,
                [FromServices] ISyncInventoryService syncInventoryService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // 仅商户管理员可批量导入（员工走单条 CRUD）
                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                {
                    return ApiResponseHelper.Forbidden("仅商户管理员可批量导入在售商品", httpContext);
                }

                if (!currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请选择要上传的 Excel 文件", httpContext: httpContext);

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".xlsx" && ext != ".xls")
                    return ApiResponseHelper.BadRequest("只支持 .xlsx 或 .xls 格式", httpContext: httpContext);

                using var stream = file.OpenReadStream();
                var result = await syncInventoryService.ImportInventoryAsync(
                    currentUser.CurrentMerchantId.Value,
                    stream,
                    file.FileName,
                    httpContext.RequestAborted);

                if (!result.Success)
                    return ApiResponseHelper.Problem("库存导入失败", result.Message, httpContext);

                return Results.Content(result.Message, "application/json");
            })
            .WithName("ImportMerchantInventory")
            .WithSummary("Excel 批量导入在售商品")
            .WithDescription("上传 Excel 批量导入在售商品（需商户管理员权限），导入数据标记为商户自管不被爬虫覆盖")
            .DisableAntiforgery();
        }

        /// <summary>
        /// 按当前商户上下文定位商户（一人多商户：X-Merchant-Id 或缺省首个在职成员商户）
        /// 取代原按 User.MerchantId 单值列的定位方式
        /// </summary>
        private static async Task<OpenFindBearings.Application.DTOs.MerchantDetailDto?> GetCurrentMerchantAsync(
            ICurrentUserService currentUser,
            IMediator mediator)
        {
            var merchantId = currentUser.CurrentMerchantId;
            if (!merchantId.HasValue)
                return null;

            var merchant = await mediator.Send(new GetMerchantQuery { Id = merchantId.Value, IsAuthenticated = true });
            return merchant;
        }
    }

    /// <summary>
    /// 变更成员角色请求体
    /// </summary>
    public record ChangeMerchantMemberRoleRequest(string Role);
}
