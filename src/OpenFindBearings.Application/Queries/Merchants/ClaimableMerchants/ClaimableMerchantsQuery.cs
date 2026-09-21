using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.ClaimableMerchants
{
    /// <summary>
    /// 入驻发现搜索查询（v2.9.0 由"仅可认领池"升级为全量匹配 + 认领可行性标记）
    /// </summary>
    public record ClaimableMerchantsQuery : IRequest<OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>>
    {
        /// <summary>
        /// 搜索关键词（商户名称/公司全称）
        /// </summary>
        public string? Keyword { get; init; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; init; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// 当前登录用户业务ID（可空——匿名时 IsMine 恒 false；用于标记"我的商户"去管理入口）
        /// </summary>
        public Guid? CurrentUserId { get; init; }
    }

    /// <summary>
    /// 入驻发现搜索查询处理器：全量匹配非草稿商户，逐条计算认领可行性——
    /// 可认领 = 未认证 && 无在职成员 && 无进行中提名锁定；在职成员含当前用户则标"我的商户"
    /// </summary>
    public class ClaimableMerchantsQueryHandler : IRequestHandler<ClaimableMerchantsQuery, OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ILogger<ClaimableMerchantsQueryHandler> _logger;

        public ClaimableMerchantsQueryHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            IStaffInvitationRepository invitationRepository,
            ILogger<ClaimableMerchantsQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _invitationRepository = invitationRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>> Handle(
            ClaimableMerchantsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("入驻发现搜索: Keyword={Keyword}", request.Keyword);

            var result = await _merchantRepository.GetDiscoverableAsync(
                request.Keyword, request.Page, request.PageSize, cancellationToken);

            var merchantIds = result.Items.Select(m => m.Id).ToList();

            // 批量取在职成员与提名锁定集合（两查询共享 DbContext，串行 await）
            var activeMembers = await _memberRepository.GetActiveByMerchantIdsAsync(merchantIds, cancellationToken);
            var lockedIds = await _invitationRepository.GetNominationLockedMerchantIdsAsync(merchantIds, cancellationToken);

            var merchantsWithMembers = activeMembers.Select(x => x.MerchantId).ToHashSet();
            var myMerchantIds = request.CurrentUserId.HasValue
                ? activeMembers.Where(x => x.UserId == request.CurrentUserId.Value).Select(x => x.MerchantId).ToHashSet()
                : [];

            var items = result.Items.Select(m =>
            {
                var isMine = myMerchantIds.Contains(m.Id);
                var claimable = !m.IsVerified
                    && !merchantsWithMembers.Contains(m.Id)
                    && !lockedIds.Contains(m.Id);

                string statusText;
                if (isMine) statusText = "我的商户";
                else if (m.Status == MerchantStatus.Pending) statusText = "审核中";
                else if (m.IsVerified) statusText = "已认证";
                else if (merchantsWithMembers.Contains(m.Id) || lockedIds.Contains(m.Id)) statusText = "已入驻";
                else statusText = "可认领";

                return new ClaimableMerchantDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    CompanyName = m.CompanyName,
                    Type = m.GetMerchantTypeDisplayName(),
                    IsClaimable = claimable,
                    IsMine = isMine,
                    StatusText = statusText
                };
            }).ToList();

            return new OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
