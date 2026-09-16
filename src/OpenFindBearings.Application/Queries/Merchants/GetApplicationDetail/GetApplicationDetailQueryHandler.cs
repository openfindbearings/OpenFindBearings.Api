using MediatR;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Queries.Merchants.GetApplicationDetail;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetApplicationDetail
{
    /// <summary>
    /// 入驻申请详情查询处理器（v2.6.0 新增，被拒重提表单预填）。
    /// 守卫：调用者须为该商户在职成员（非成员与不存在同样返回 null，端点映射 404，不泄露存在性）。
    /// </summary>
    public class GetApplicationDetailQueryHandler : IRequestHandler<GetApplicationDetailQuery, MerchantApplicationDetailDto?>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;

        public GetApplicationDetailQueryHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
        }

        public async Task<MerchantApplicationDetailDto?> Handle(
            GetApplicationDetailQuery request,
            CancellationToken cancellationToken)
        {
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (member == null)
                return null;

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
                return null;

            return new MerchantApplicationDetailDto
            {
                MerchantId = merchant.Id,
                MerchantName = merchant.Name,
                Status = merchant.Status.ToString(),
                RejectReason = merchant.Status == MerchantStatus.Suspended
                    ? merchant.SuspensionReason
                    : null,
                ApplicationMode = merchant.ApplicationMode.ToString().ToLowerInvariant(),
                Role = member.Role,
                Type = (int)merchant.Type,
                CompanyName = merchant.CompanyName,
                UnifiedSocialCreditCode = merchant.UnifiedSocialCreditCode,
                ContactPerson = merchant.Contact?.ContactPerson,
                Phone = merchant.Contact?.Phone,
                Mobile = merchant.Contact?.Mobile,
                Email = merchant.Contact?.Email,
                Address = merchant.Contact?.Address,
                Description = merchant.Description,
                LogoUrl = merchant.LogoUrl
            };
        }
    }
}
