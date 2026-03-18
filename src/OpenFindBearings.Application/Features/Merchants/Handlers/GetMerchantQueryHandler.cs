using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 获取商家查询处理器
    /// </summary>
    public class GetMerchantQueryHandler : IRequestHandler<GetMerchantQuery, MerchantDetailDto?>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetMerchantQueryHandler> _logger;

        public GetMerchantQueryHandler(
            IMerchantRepository merchantRepository,
            ILogger<GetMerchantQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<MerchantDetailDto?> Handle(GetMerchantQuery request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
                return null;

            return MapToDetailDto(merchant);
        }

        private MerchantDetailDto MapToDetailDto(Domain.Entities.Merchant merchant)
        {
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
                VerifiedAt = merchant.VerifiedAt,
                Staff = merchant.Staff?.Select(s => new MerchantStaffDto
                {
                    Id = s.Id,
                    Nickname = s.Nickname ?? string.Empty,
                    Email = s.Email,
                    Phone = s.Phone,
                    Avatar = s.Avatar
                }).ToList() ?? new(),
                Products = new List<MerchantBearingDto>() // TODO: 填充产品列表
            };
        }
    }
}
