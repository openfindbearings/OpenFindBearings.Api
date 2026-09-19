using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.Merchants.AcceptNomination;
using OpenFindBearings.Application.Commands.Merchants.ApplyMerchant;
using OpenFindBearings.Application.Commands.Merchants.DeleteRejectedApplication;
using OpenFindBearings.Application.Commands.Merchants.NominateMerchant;
using OpenFindBearings.Application.Commands.Merchants.ResubmitApplication;
using OpenFindBearings.Application.Commands.Merchants.WithdrawApplication;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Queries.Merchants.ClaimableMerchants;
using OpenFindBearings.Application.Queries.Merchants.GetApplicationDetail;
using OpenFindBearings.Application.Queries.Merchants.GetMerchantApplication;
using OpenFindBearings.Application.Queries.Merchants.PendingNominations;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 商户入驻端点（登录用户自助申请/认领/状态查询）
    /// </summary>
    public static class MerchantApplicationEndpoints
    {
        public static void MapMerchantApplicationEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/merchant")
                .WithTags("商户入驻")
                .RequireAuthorization();

            /// <summary>
            /// 提交入驻申请（self 新建 / claim 认领爬虫商家）
            /// </summary>
            group.MapPost("/apply", async (
                ApplyMerchantRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new ApplyMerchantCommand
                {
                    Mode = request.Mode,
                    ClaimMerchantId = request.ClaimMerchantId,
                    Name = request.Name,
                    Type = request.Type,
                    ContactPerson = request.ContactPerson,
                    Phone = request.Phone,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    Address = request.Address,
                    CompanyName = request.CompanyName,
                    UnifiedSocialCreditCode = request.UnifiedSocialCreditCode,
                    Description = request.Description,
                    Documents = request.Documents,
                    ApplicantUserId = currentUser.UserId.Value
                };

                var merchantId = await mediator.Send(command);
                return ApiResponseHelper.Ok(
                    new { merchantId, message = "入驻申请已提交，等待审核" },
                    httpContext: httpContext);
            })
            .WithName("ApplyMerchant")
            .WithSummary("提交入驻申请")
            .WithDescription("提交商户入驻申请（self 新建或 claim 认领爬虫商家），申请人成为该商户管理员");

            /// <summary>
            /// 查询当前用户在各商户的入驻状态
            /// </summary>
            group.MapGet("/application", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var result = await mediator.Send(new GetMerchantApplicationQuery(currentUser.UserId.Value));
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMerchantApplication")
            .WithSummary("查询入驻状态")
            .WithDescription("查询当前用户在各商户的入驻进度（待审核/已生效/已拒绝）");

            /// <summary>
            /// 申请人自助撤回待审核的入驻申请（self 新建硬删 / claim 认领退回爬虫）
            /// </summary>
            group.MapPost("/{merchantId:guid}/withdraw", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new WithdrawApplicationCommand
                {
                    MerchantId = merchantId,
                    ApplicantUserId = currentUser.UserId.Value
                });

                return ApiResponseHelper.Ok("入驻申请已撤回", httpContext: httpContext);
            })
            .WithName("WithdrawMerchantApplication")
            .WithSummary("撤回入驻申请")
            .WithDescription("申请人撤回自己待审核的入驻申请：新建商户将被删除，认领的商家退回公共池可再被认领");

            /// <summary>
            /// 查询单个入驻申请详情（被拒重提表单预填，v2.6.0 新增）
            /// </summary>
            group.MapGet("/{merchantId:guid}/application", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var detail = await mediator.Send(new GetApplicationDetailQuery(currentUser.UserId.Value, merchantId));
                if (detail == null)
                    return ApiResponseHelper.NotFound("申请不存在或你已不是该商户成员", httpContext: httpContext);

                return ApiResponseHelper.Ok(detail, httpContext: httpContext);
            })
            .WithName("GetMerchantApplicationDetail")
            .WithSummary("查询入驻申请详情")
            .WithDescription("申请人查看自己某张入驻申请的全量资料（被拒后修改重提的预填数据源）");

            /// <summary>
            /// 被拒后修改资料重新提交（v2.6.0 新增，self/claim 通道）
            /// </summary>
            group.MapPost("/{merchantId:guid}/resubmit", async (
                Guid merchantId,
                ResubmitApplicationRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new ResubmitApplicationCommand
                {
                    MerchantId = merchantId,
                    ApplicantUserId = currentUser.UserId.Value,
                    Name = request.Name,
                    Type = request.Type,
                    ContactPerson = request.ContactPerson,
                    Phone = request.Phone,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    Address = request.Address,
                    CompanyName = request.CompanyName,
                    UnifiedSocialCreditCode = request.UnifiedSocialCreditCode,
                    Description = request.Description,
                    Documents = request.Documents
                };

                await mediator.Send(command);
                return ApiResponseHelper.Ok(
                    new { merchantId, message = "修改后的申请已重新提交，等待审核" },
                    httpContext: httpContext);
            })
            .WithName("ResubmitMerchantApplication")
            .WithSummary("重新提交被拒的入驻申请")
            .WithDescription("申请人修改资料后重新提交被驳回的入驻申请（Suspended→Pending 重走审核），仅 self/claim 渠道开放");

            /// <summary>
            /// 删除被驳回的入驻申请（v2.6.0 新增，与撤回同构：Self 硬删 / Claim 退回认领池）
            /// </summary>
            group.MapPost("/{merchantId:guid}/delete-application", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new DeleteRejectedApplicationCommand
                {
                    MerchantId = merchantId,
                    ApplicantUserId = currentUser.UserId.Value
                });

                return ApiResponseHelper.Ok("被驳回的申请已删除", httpContext: httpContext);
            })
            .WithName("DeleteMerchantApplication")
            .WithSummary("删除被驳回的入驻申请")
            .WithDescription("申请人删除自己被驳回的入驻申请：新建商户将被彻底删除，认领的商家退回公共池可再被认领");


            /// <summary>
            /// 提名他人为管理员（入驻模式 B）
            /// </summary>
            group.MapPost("/nominate", async (
                NominateMerchantRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new NominateMerchantCommand
                {
                    InitiatorUserId = currentUser.UserId.Value,
                    NomineePhone = request.NomineePhone,
                    NomineeEmail = request.NomineeEmail,
                    Name = request.Name,
                    Type = request.Type,
                    CompanyName = request.CompanyName,
                    ContactPerson = request.ContactPerson,
                    Phone = request.Phone,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    Address = request.Address,
                    InitiatorJoins = request.InitiatorJoins,
                    TargetMerchantId = request.TargetMerchantId
                };

                var code = await mediator.Send(command);
                return ApiResponseHelper.Ok(
                    new { code, message = "提名邀请已创建，等待对方接受" },
                    httpContext: httpContext);
            })
            .WithName("NominateMerchant")
            .WithSummary("提名他人为管理员")
            .WithDescription("提名另一手机号/邮箱作为商户管理员，被提名人接受并补资料后进入审核");

            /// <summary>
            /// 接受管理员提名（被提名人补全资料并提交审核）
            /// </summary>
            group.MapPost("/nominate/{code}/accept", async (
                string code,
                AcceptNominationRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new AcceptNominationCommand
                {
                    InvitationCode = code,
                    NomineeUserId = currentUser.UserId.Value,
                    // 安全强化：被提名人手机号取自 JWT phone_number claim（服务端可信），
                    // handler 校验与邀请手机号一致，防止拿到邀请码的任意用户冒领管理员身份
                    NomineePhone = httpContext.User.FindFirst("phone_number")?.Value,
                    ContactPerson = request.ContactPerson,
                    Phone = request.Phone,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    Address = request.Address,
                    CompanyName = request.CompanyName,
                    UnifiedSocialCreditCode = request.UnifiedSocialCreditCode,
                    Description = request.Description,
                    Documents = request.Documents
                };

                var merchantId = await mediator.Send(command);
                return ApiResponseHelper.Ok(
                    new { merchantId, message = "已接受提名，申请已提交，等待审核" },
                    httpContext: httpContext);
            })
            .WithName("AcceptNomination")
            .WithSummary("接受管理员提名")
            .WithDescription("被提名人接受提名并补全商户资料，提交后台审核");

            /// <summary>
            /// 认领搜索爬虫商家
            /// </summary>
            group.MapGet("/claimable", async (
                [FromQuery] string? keyword,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(new ClaimableMerchantsQuery
                {
                    Keyword = keyword,
                    Page = page,
                    PageSize = pageSize
                });

                return ApiResponseHelper.Paged(
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext);
            })
            .WithName("GetClaimableMerchants")
            .WithSummary("认领搜索爬虫商家")
            .WithDescription("搜索可认领的爬虫来源商家（未被认领）");

            /// <summary>
            /// 待我接受的管理员提名列表（G2）
            /// </summary>
            group.MapGet("/nominations/pending", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                // 手机号只信 JWT claim（不接收户参数）：用户仅能看到发给自己的提名
                var phone = httpContext.User.FindFirst("phone_number")?.Value;
                if (string.IsNullOrEmpty(phone))
                    return ApiResponseHelper.Ok(new List<PendingNominationDto>(), httpContext: httpContext);

                var list = await mediator.Send(new GetPendingNominationsQuery { Phone = phone });
                return ApiResponseHelper.Ok(list, httpContext: httpContext);
            })
            .WithName("GetPendingNominations")
            .WithSummary("待我接受的提名")
            .WithDescription("按当前登录用户手机号匹配待接受的管理员提名邀请");
        }
    }

    /// <summary>
    /// 入驻申请请求体
    /// </summary>
    public record ApplyMerchantRequest(
        string Mode = "self",
        Guid? ClaimMerchantId = null,
        string? Name = null,
        int? Type = null,
        string? ContactPerson = null,
        string? Phone = null,
        string? Mobile = null,
        string? Email = null,
        string? Address = null,
        string? CompanyName = null,
        string? UnifiedSocialCreditCode = null,
        string? Description = null,
        IReadOnlyList<DocumentSubmission>? Documents = null);

    /// <summary>
    /// 被拒后修改重提请求体（字段与入驻申请一致，全量提交、服务端字段级合并）
    /// </summary>
    public record ResubmitApplicationRequest(
        string? Name = null,
        int? Type = null,
        string? ContactPerson = null,
        string? Phone = null,
        string? Mobile = null,
        string? Email = null,
        string? Address = null,
        string? CompanyName = null,
        string? UnifiedSocialCreditCode = null,
        string? Description = null,
        IReadOnlyList<DocumentSubmission>? Documents = null);

    /// <summary>
    /// 提名他人为管理员请求体
    /// </summary>
    public record NominateMerchantRequest(
        string? NomineePhone = null,
        string? NomineeEmail = null,
        string? Name = null,
        int? Type = null,
        string? CompanyName = null,
        string? ContactPerson = null,
        string? Phone = null,
        string? Mobile = null,
        string? Email = null,
        string? Address = null,
        bool InitiatorJoins = true,
        Guid? TargetMerchantId = null);

    /// <summary>
    /// 接受提名请求体（被提名人补全资料）
    /// </summary>
    public record AcceptNominationRequest(
        string? ContactPerson = null,
        string? Phone = null,
        string? Mobile = null,
        string? Email = null,
        string? Address = null,
        string? CompanyName = null,
        string? UnifiedSocialCreditCode = null,
        string? Description = null,
        IReadOnlyList<DocumentSubmission>? Documents = null);
}
