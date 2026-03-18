using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Follows.DTOs;
using OpenFindBearings.Application.Features.Follows.Queries;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Follows.Handlers
{
    /// <summary>
    /// 获取我关注的商家列表查询处理器
    /// </summary>
    public class GetMyFollowedMerchantsQueryHandler : IRequestHandler<GetMyFollowedMerchantsQuery, PagedResult<FollowedMerchantDto>>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMyFollowedMerchantsQueryHandler> _logger;

        public GetMyFollowedMerchantsQueryHandler(
            IUserMerchantFollowRepository followRepository,
            IUserRepository userRepository,
            ILogger<GetMyFollowedMerchantsQueryHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<FollowedMerchantDto>> Handle(GetMyFollowedMerchantsQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return new PagedResult<FollowedMerchantDto>();
            }

            var follows = await _followRepository.GetByUserIdAsync(user.Id, request.Page, request.PageSize, cancellationToken);
            var totalCount = await _followRepository.CountByUserIdAsync(user.Id, cancellationToken);

            var items = new List<FollowedMerchantDto>();
            foreach (var follow in follows)
            {
                if (follow.Merchant == null) continue;  // 处理 null 情况

                items.Add(new FollowedMerchantDto
                {
                    Id = follow.Id,
                    CreatedAt = follow.CreatedAt,
                    Merchant = new MerchantDto
                    {
                        Id = follow.Merchant.Id,
                        Name = follow.Merchant.Name,
                        CompanyName = follow.Merchant.CompanyName,
                        Type = follow.Merchant.Type.ToString(),
                        ContactPerson = follow.Merchant.Contact?.ContactPerson,
                        Phone = follow.Merchant.Contact?.Phone,
                        Mobile = follow.Merchant.Contact?.Mobile,
                        Email = follow.Merchant.Contact?.Email,
                        Address = follow.Merchant.Contact?.Address,
                        IsVerified = follow.Merchant.IsVerified,
                        Grade = follow.Merchant.Grade.ToString(),
                        FollowerCount = follow.Merchant.FollowerCount,
                        ProductCount = follow.Merchant.MerchantBearings?.Count ?? 0
                    }
                });
            }

            return new PagedResult<FollowedMerchantDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
