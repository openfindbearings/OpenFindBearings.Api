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
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetMerchantByUserIdQueryHandler> _logger;

        public GetMerchantByUserIdQueryHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetMerchantByUserIdQueryHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<MerchantDetailDto?> Handle(GetMerchantByUserIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("根据用户ID获取商家: UserId={UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || !user.MerchantId.HasValue)
                return null;

            var merchant = await _merchantRepository.GetByIdAsync(user.MerchantId.Value, cancellationToken);
            if (merchant == null)
                return null;

            return merchant.ToDetailDto();
        }
    }
}
