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
using OpenFindBearings.Application.Commands.Merchants.StaffInvitationActions;
using OpenFindBearings.Application.Queries.Merchants.PendingStaffInvitations;
using OpenFindBearings.Application.Commands.Merchants.SubmitDocument;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Enums;
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
            /// 获取店铺信息（商户信息维护页读接口）
            /// </summary>
            group.MapGet("/profile", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // 改动说明：改用当前商户上下文(X-Merchant-Id)定位，与 PUT /profile 一致。
                //   原用 GetMerchantByUserIdQuery 取"成员列表首个"，多商户下会 A 载入、B 保存，读写错位。
                var result = await GetCurrentMerchantAsync(currentUser, mediator);

                return result == null
                    ? ApiResponseHelper.NotFound("未找到所属商家", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMerchantProfile")
            .WithSummary("获取店铺信息")
            .WithDescription("获取当前商户上下文的店铺详细信息（供商户信息维护页编辑回填）");

            /// <summary>
            /// 更新店铺信息（商户信息维护页写接口，需商户管理员）
            /// </summary>
            group.MapPut("/profile", async (
                UpdateMerchantCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPermissionService permissionService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // 改动说明：资料维护锁定为商户管理员专属（对齐成员管理/批量导入端点的 admin 校验）
                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);

                var merchant = await GetCurrentMerchantAsync(currentUser, mediator);

                if (merchant == null)
                    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);

                var updateCommand = command with { Id = merchant.Id };
                await mediator.Send(updateCommand);

                return ApiResponseHelper.Ok("店铺信息更新成功", httpContext);
            })
            .WithName("UpdateMerchantProfile")
            .WithSummary("更新店铺信息")
            .WithDescription("更新当前商户的资料（需商家管理员权限）");

            /// <summary>
            /// 上传商户 Logo（图片文件，仅返回可访问 URL；由维护页保存时随 profile 落库）
            /// </summary>
            group.MapPost("/logo", async (
                IFormFile file,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IPermissionService permissionService,
                [FromServices] IObjectStorageService storage,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // 改动说明：Logo 属商户资料，上传同样限商户管理员
                var isAdmin = await permissionService.IsMerchantAdmin();
                if (!isAdmin)
                    return ApiResponseHelper.Forbidden("需要商家管理员权限", httpContext);

                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请上传文件", httpContext: httpContext);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                // 改动说明（v1.7.1）：扩展名缺失时按 MIME 推断（RN 客户端 multipart 文件名无扩展名）
                var fileExtension = FileUploadHelper.GetSafeExtension(file);
                if (!allowedExtensions.Contains(fileExtension))
                    return ApiResponseHelper.BadRequest("只支持 JPG、PNG、WEBP 图片格式", httpContext: httpContext);

                if (file.Length > 2 * 1024 * 1024)
                    return ApiResponseHelper.BadRequest("图片大小不能超过 2MB", httpContext: httpContext);

                try
                {
                    // 改动说明（v1.5.0）：落盘从直写 wwwroot 改走 IObjectStorageService（生产 MinIO / 开发本地盘），
                    //   key 与 URL 形态不变（uploads/merchants/logo/...），展示层无感；
                    //   仅回相对 URL，DB 写入由维护页保存 profile 时带 logoUrl 完成（null 保留、非空覆盖）
                    using var buffer = new MemoryStream();
                    await file.CopyToAsync(buffer);
                    var fileName = $"{currentUser.UserId.Value:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                    var fileUrl = await storage.UploadAsync(
                        $"uploads/merchants/logo/{fileName}", buffer.ToArray(),
                        FileUploadHelper.ContentTypeFromExtension(fileExtension));
                    return ApiResponseHelper.Ok(new { url = fileUrl, message = "Logo 上传成功" }, httpContext: httpContext);
                }
                catch (Exception ex)
                {
                    return ApiResponseHelper.Problem("Logo 上传失败", ex.Message, httpContext: httpContext);
                }
            })
            .WithName("UploadMerchantLogo")
            .WithSummary("上传商户Logo")
            .WithDescription("上传商户 Logo 图片，返回可访问 URL（需商家管理员权限，保存资料时落库）")
            .DisableAntiforgery();

            /// <summary>
            /// 上传证照材料（v2.7.0 由"上传营业执照"泛化：type 区分执照/授权书/厂房照，绑定当前商户上下文）
            /// </summary>
            group.MapPost("/documents", async (
            IFormFile file,
            [FromForm] int type,
            [FromServices] ICurrentUserService currentUser,
            [FromServices] IMediator mediator,
            [FromServices] IObjectStorageService storage,
            HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请上传文件", httpContext: httpContext);

                // 改动说明（v2.7.0）：材料类型服务端校验，非法值直接拒
                if (!Enum.IsDefined(typeof(DocumentType), type))
                    return ApiResponseHelper.BadRequest("材料类型无效", httpContext: httpContext);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                // 改动说明（v1.7.1）：扩展名缺失时按 MIME 推断（RN 客户端 multipart 文件名无扩展名）
                var fileExtension = FileUploadHelper.GetSafeExtension(file);
                if (!allowedExtensions.Contains(fileExtension))
                    return ApiResponseHelper.BadRequest("只支持 JPG、PNG、PDF 格式", httpContext: httpContext);

                if (file.Length > 5 * 1024 * 1024)
                    return ApiResponseHelper.BadRequest("文件大小不能超过5MB", httpContext: httpContext);

                try
                {
                    // 改动说明（v2.7.0）：商户定位由"首个成员商户"改为当前商户上下文头 X-Merchant-Id，
                    //   多商户用户换材料不再错绑到第一个商户
                    if (!currentUser.CurrentMerchantId.HasValue)
                        return ApiResponseHelper.BadRequest("请先选择当前商户", httpContext: httpContext);
                    var merchantId = currentUser.CurrentMerchantId.Value;

                    // 改动说明（v1.5.0）：落盘改走 IObjectStorageService（生产 MinIO / 开发本地盘），key/URL 形态不变
                    // 时区规范修复：文件名时间戳统一 UTC（原 DateTime.Now 依赖服务器时区）
                    using var buffer = new MemoryStream();
                    await file.CopyToAsync(buffer);
                    var fileName = $"{merchantId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                    var fileUrl = await storage.UploadAsync(
                        $"uploads/documents/{fileName}", buffer.ToArray(),
                        FileUploadHelper.ContentTypeFromExtension(fileExtension));

                    var documentCommand = new SubmitDocumentCommand
                    {
                        MerchantId = merchantId,
                        Type = (DocumentType)type,
                        FileUrl = fileUrl,
                        SubmittedBy = currentUser.UserId.Value
                    };
                    var documentId = await mediator.Send(documentCommand);

                    return ApiResponseHelper.Ok(new
                    {
                        documentId,
                        url = fileUrl,
                        message = $"{Application.DTOs.DocumentRequirements.DisplayName((DocumentType)type)}已提交，等待审核"
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
            .WithName("UploadDocument")
            .WithSummary("上传证照材料")
            .WithDescription("按类型上传商户证照材料（1 营业执照 / 2 品牌授权书 / 3 厂房照片）进入审核队列，绑定 X-Merchant-Id 当前商户")
            .DisableAntiforgery();

            /// <summary>
            /// 当前商户证照材料列表（v2.7.0 新增，商户信息维护页"证照材料"区读自身材料状态）
            /// </summary>
            group.MapGet("/documents", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // CurrentMerchantId 属性本身已按成员表校验归属（非法/非成员商户为 null）
                if (!currentUser.CurrentMerchantId.HasValue)
                    return ApiResponseHelper.BadRequest("请先选择当前商户", httpContext: httpContext);

                var result = await mediator.Send(new OpenFindBearings.Application.Queries.Admin.GetMerchantDocuments.GetMerchantDocumentsQuery
                {
                    MerchantId = currentUser.CurrentMerchantId.Value
                });
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMyMerchantDocuments")
            .WithSummary("获取当前商户证照材料")
            .WithDescription("返回当前商户上下文（X-Merchant-Id）的全部证照材料与审核状态");

            /// <summary>
            /// 材料文件纯上传（v2.7.0 新增）：只落盘返回 URL、不建审核记录。
            /// 入驻申请"随单材料"先经此换取 fileUrl，再放入 apply/resubmit/accept 的 documents 数组统一建单；
            /// 入驻后的即时提交仍走 POST /documents（带 type 直接建待审记录）。
            /// </summary>
            group.MapPost("/documents/upload", async (
            IFormFile file,
            [FromServices] ICurrentUserService currentUser,
            [FromServices] IObjectStorageService storage,
            HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请上传文件", httpContext: httpContext);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                // 改动说明（v1.7.1）：扩展名缺失时按 MIME 推断（RN 客户端 multipart 文件名无扩展名）
                var fileExtension = FileUploadHelper.GetSafeExtension(file);
                if (!allowedExtensions.Contains(fileExtension))
                    return ApiResponseHelper.BadRequest("只支持 JPG、PNG、PDF 格式", httpContext: httpContext);

                if (file.Length > 5 * 1024 * 1024)
                    return ApiResponseHelper.BadRequest("文件大小不能超过5MB", httpContext: httpContext);

                try
                {
                    // 改动说明（v1.5.0）：预上传同样走 IObjectStorageService，key/URL 形态不变
                    // 时区规范：文件名时间戳统一 UTC
                    using var buffer = new MemoryStream();
                    await file.CopyToAsync(buffer);
                    var fileName = $"pre_{currentUser.UserId.Value:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                    var fileUrl = await storage.UploadAsync(
                        $"uploads/documents/{fileName}", buffer.ToArray(),
                        FileUploadHelper.ContentTypeFromExtension(fileExtension));
                    return ApiResponseHelper.Ok(new { url = fileUrl }, httpContext: httpContext);
                }
                catch (Exception ex)
                {
                    return ApiResponseHelper.Problem(title: "上传失败", detail: ex.Message, httpContext: httpContext);
                }
            })
            .WithName("UploadDocumentFile")
            .WithSummary("材料文件预上传")
            .WithDescription("上传材料图片/PDF 返回可访问 URL（不建审核记录，供入驻申请随单材料先传后提交）")
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

                // 改动说明（v1.5.2）：传当前用户 id 供 DTO 标记 isSelf
                var staffList = await mediator.Send(new GetMerchantStaffQuery(merchant.Id, currentUser.UserId));

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
                // 改动说明（v2.9.0 邀请确认制）：返回真实结果文案（邀请已发送/已是在职成员），
                //   原硬编码"员工添加成功"与静默拉入语义一并废弃
                return ApiResponseHelper.Ok(new { id = result, message = result.Message ?? "邀请已发送，对方同意后加入" }, httpContext: httpContext);
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
            /// 待我确认的员工邀请列表（v2.9.0 邀请确认制：按 JWT 手机号匹配，登录后商户页横幅消费）
            /// </summary>
            group.MapGet("/staff/invitations/pending", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var items = await mediator.Send(new GetPendingStaffInvitationsQuery { Phone = currentUser.Phone, Email = currentUser.Email });
                return ApiResponseHelper.Ok(items, httpContext: httpContext);
            })
            .WithName("GetPendingStaffInvitations")
            .WithSummary("待我确认的员工邀请")
            .WithDescription("被邀人查看发给自己的待确认商户邀请（按 JWT 手机号匹配）");

            /// <summary>
            /// 接受员工邀请（v2.9.0：建成员行入伙 + 通知发起人；手机号服务端比对防撞领）
            /// </summary>
            group.MapPost("/staff/invitations/{invitationId:guid}/accept", async (
                Guid invitationId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue || string.IsNullOrEmpty(currentUser.AuthUserId))
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new AcceptStaffInvitationCommand
                {
                    InvitationId = invitationId,
                    UserId = currentUser.UserId.Value,
                    AuthUserId = currentUser.AuthUserId,
                    Phone = currentUser.Phone ?? string.Empty
                });
                return ApiResponseHelper.Ok("已接受邀请，正式加入商户", httpContext: httpContext);
            })
            .WithName("AcceptStaffInvitation")
            .WithSummary("接受员工邀请")
            .WithDescription("被邀人同意加入商户，创建成员行并通知发起人");

            /// <summary>
            /// 拒绝员工邀请（v2.9.0：邀请置 Declined，不建成员行）
            /// </summary>
            group.MapPost("/staff/invitations/{invitationId:guid}/decline", async (
                Guid invitationId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new DeclineStaffInvitationCommand
                {
                    InvitationId = invitationId,
                    Phone = currentUser.Phone ?? string.Empty
                });
                return ApiResponseHelper.Ok("已拒绝邀请", httpContext: httpContext);
            })
            .WithName("DeclineStaffInvitation")
            .WithSummary("拒绝员工邀请")
            .WithDescription("被邀人拒绝商户邀请");

            /// <summary>
            /// 撤销员工邀请（v2.9.0：管理员撤回自己商户发出的待确认邀请，成员列表"已邀请"行操作）
            /// </summary>
            group.MapPost("/staff/invitations/{invitationId:guid}/revoke", async (
                Guid invitationId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new RevokeStaffInvitationCommand
                {
                    InvitationId = invitationId,
                    OperatorId = currentUser.UserId.Value
                });
                return ApiResponseHelper.Ok("邀请已撤销", httpContext: httpContext);
            })
            .WithName("RevokeStaffInvitation")
            .WithSummary("撤销员工邀请")
            .WithDescription("商户管理员撤销待确认的员工邀请（需管理员权限）");

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

                // 改动说明（v1.7.1）：同材料端点，无扩展名按 MIME 推断
                var ext = FileUploadHelper.GetSafeExtension(file);
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
