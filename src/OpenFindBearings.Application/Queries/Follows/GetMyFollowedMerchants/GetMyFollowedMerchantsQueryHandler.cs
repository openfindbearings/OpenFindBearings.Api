using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Follows.GetMyFollowedMerchants
{
    /// <summary>
    /// 获取我关注的商家列表查询处理器
    /// </summary>
    public class GetMyFollowedMerchantsQueryHandler : IRequestHandler<GetMyFollowedMerchantsQuery, PagedResult<FollowedMerchantDto>>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ILogger<GetMyFollowedMerchantsQueryHandler> _logger;

        public GetMyFollowedMerchantsQueryHandler(
            IUserMerchantFollowRepository followRepository,
            ILogger<GetMyFollowedMerchantsQueryHandler> logger)
        {
            _followRepository = followRepository;
            _logger = logger;
        }

        public async Task<PagedResult<FollowedMerchantDto>> Handle(
            GetMyFollowedMerchantsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户关注商家列表: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                request.UserId, request.Page, request.PageSize);

            var follows = await _followRepository.GetByUserIdAsync(
                request.UserId,
                request.Page,
                request.PageSize,
                cancellationToken);

            var totalCount = await _followRepository.CountByUserIdAsync(request.UserId, cancellationToken);

            var items = follows.Select(f => new FollowedMerchantDto
            {
                Id = f.Id,
                CreatedAt = f.CreatedAt,
                Merchant = f.Merchant != null ? new MerchantDto
                {
                    Id = f.Merchant.Id,
                    Name = f.Merchant.Name,
                    CompanyName = f.Merchant.CompanyName,
                    Type = f.Merchant.Type.ToString(),
                    ContactPerson = f.Merchant.Contact?.ContactPerson,
                    Phone = f.Merchant.Contact?.Phone,
                    Mobile = f.Merchant.Contact?.Mobile,
                    Email = f.Merchant.Contact?.Email,
                    Address = f.Merchant.Contact?.Address,
                    IsVerified = f.Merchant.IsVerified,
                    Grade = f.Merchant.Grade.ToString(),
                    FollowerCount = f.Merchant.FollowerCount,
                    ProductCount = f.Merchant.MerchantBearings?.Count ?? 0
                } : null!
            }).ToList();

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
