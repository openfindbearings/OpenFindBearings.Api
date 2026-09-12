using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantByUserId
{
    /// <summary>
    /// 根据用户ID获取商家查询处理器
    /// </summary>
    public class GetMerchantByUserIdQueryHandler : IRequestHandler<GetMerchantByUserIdQuery, MerchantDetailDto?>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetMerchantByUserIdQueryHandler> _logger;

        public GetMerchantByUserIdQueryHandler(
            IMerchantMemberRepository merchantMemberRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetMerchantByUserIdQueryHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<MerchantDetailDto?> Handle(GetMerchantByUserIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("根据用户ID获取商家: UserId={UserId}", request.UserId);

            // 改动说明：由 User.MerchantId 单值列改为按成员表定位（用户首个在职成员商户），支持一人多商户
            var members = await _merchantMemberRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
            if (members.Count == 0)
                return null;

            var merchant = await _merchantRepository.GetByIdAsync(members[0].MerchantId, cancellationToken);
            if (merchant == null)
                return null;

            return merchant.ToDetailDto();
        }
    }
}
