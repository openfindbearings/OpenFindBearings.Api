using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Favorites.GetMyFavoriteBearings
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
            _logger.LogInformation("获取用户收藏列表: UserId={UserId}, Page={Page}, PageSize={PageSize}", request.UserId, request.Page, request.PageSize);

            var favorites = await _favoriteRepository.GetByUserIdAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
            var totalCount = await _favoriteRepository.CountByUserIdAsync(request.UserId, cancellationToken);

            var items = favorites
                .Where(f => f.Bearing != null)
                .Select(f => f.ToDto())
                .ToList();

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
