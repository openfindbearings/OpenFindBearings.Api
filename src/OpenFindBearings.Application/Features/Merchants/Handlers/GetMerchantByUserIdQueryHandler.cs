using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
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

            // 映射到 DTO
            return new MerchantDetailDto
            {
                Id = merchant.Id,
                Name = merchant.Name,
                CompanyName = merchant.CompanyName,
                Type = merchant.Type.ToString(),
                ContactPerson = merchant.Contact?.ContactPerson,
                Phone = merchant.Contact?.Phone,
                Mobile = merchant.Contact?.Mobile,
                Email = merchant.Contact?.Email,
                Address = merchant.Contact?.Address,
                IsVerified = merchant.IsVerified,
                Grade = merchant.Grade.ToString(),
                FollowerCount = merchant.FollowerCount,
                ProductCount = merchant.MerchantBearings?.Count ?? 0,
                Description = merchant.Description,
                BusinessScope = merchant.BusinessScope,
                VerifiedAt = merchant.VerifiedAt
            };
        }
    }
}
