using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Favorites.DTOs;
using OpenFindBearings.Application.Features.Favorites.Queries;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Favorites.Handlers
{
    /// <summary>
    /// 获取我的收藏轴承列表查询处理器
    /// </summary>
    public class GetMyFavoriteBearingsQueryHandler : IRequestHandler<GetMyFavoriteBearingsQuery, PagedResult<FavoriteBearingDto>>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMyFavoriteBearingsQueryHandler> _logger;

        public GetMyFavoriteBearingsQueryHandler(
            IUserBearingFavoriteRepository favoriteRepository,
            IUserRepository userRepository,
            ILogger<GetMyFavoriteBearingsQueryHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<FavoriteBearingDto>> Handle(GetMyFavoriteBearingsQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return new PagedResult<FavoriteBearingDto>();
            }

            var favorites = await _favoriteRepository.GetByUserIdAsync(user.Id, request.Page, request.PageSize, cancellationToken);
            var totalCount = await _favoriteRepository.CountByUserIdAsync(user.Id, cancellationToken);

            var items = new List<FavoriteBearingDto>();
            foreach (var favorite in favorites)
            {
                if (favorite.Bearing == null) continue;  // 处理 null 情况

                items.Add(new FavoriteBearingDto
                {
                    Id = favorite.Id,
                    CreatedAt = favorite.CreatedAt,
                    Bearing = new BearingDto
                    {
                        Id = favorite.Bearing.Id,
                        PartNumber = favorite.Bearing.PartNumber,
                        Name = favorite.Bearing.Name,
                        InnerDiameter = favorite.Bearing.Dimensions.InnerDiameter,
                        OuterDiameter = favorite.Bearing.Dimensions.OuterDiameter,
                        Width = favorite.Bearing.Dimensions.Width,
                        BrandId = favorite.Bearing.BrandId,
                        BrandName = favorite.Bearing.Brand?.Name ?? string.Empty,
                        BearingTypeId = favorite.Bearing.BearingTypeId,
                        BearingTypeName = favorite.Bearing.BearingType?.Name ?? string.Empty,
                        ViewCount = favorite.Bearing.ViewCount,
                        FavoriteCount = favorite.Bearing.FavoriteCount
                    }
                });
            }

            return new PagedResult<FavoriteBearingDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
