using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class UserExtensions
    {
        public static UserDto ToDto(this User user, List<string>? roles = null, List<string>? permissions = null)
        {
            return new UserDto
            {
                Id = user.Id,
                AuthUserId = user.AuthUserId,
                Nickname = user.Nickname,
                Avatar = user.Avatar,
                UserType = user.Level.ToString(),
                Occupation = user.Occupation,
                CompanyName = user.CompanyName,
                Industry = user.Industry,
                MerchantId = user.MerchantId,
                MerchantName = user.Merchant?.Name,
                Roles = roles ?? new List<string>(),
                Permissions = permissions ?? new List<string>(),
                FavoriteCount = user.FavoriteCount,
                FollowCount = user.FollowCount,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                CorrectionCount = 0
            };
        }

        public static FavoriteBearingDto ToDto(this UserBearingFavorite favorite)
        {
            return new FavoriteBearingDto
            {
                Id = favorite.Id,
                CreatedAt = favorite.CreatedAt,
                Bearing = favorite.Bearing?.ToDto() ?? new BearingDto()
            };
        }

        public static FollowedMerchantDto ToDto(this UserMerchantFollow follow)
        {
            return new FollowedMerchantDto
            {
                Id = follow.Id,
                CreatedAt = follow.CreatedAt,
                Merchant = follow.Merchant?.ToPublicDto() ?? new MerchantDto()
            };
        }

        public static BearingHistoryDto ToDto(this UserBearingHistory history)
        {
            return new BearingHistoryDto
            {
                Id = history.Id,
                BearingId = history.BearingId,
                BearingCurrentCode = history.Bearing?.CurrentCode ?? string.Empty,
                BearingName = history.Bearing?.Name ?? string.Empty,
                BrandName = history.Bearing?.Brand?.Name,
                ViewedAt = history.ViewedAt,
                ViewCount = history.User?.BearingHistory?.Count(h => h.BearingId == history.BearingId) ?? 1
            };
        }

        public static MerchantHistoryDto ToDto(this UserMerchantHistory history)
        {
            return new MerchantHistoryDto
            {
                Id = history.Id,
                MerchantId = history.MerchantId,
                MerchantName = history.Merchant?.Name ?? string.Empty,
                CompanyName = history.Merchant?.CompanyName,
                ViewedAt = history.ViewedAt,
                ViewCount = history.User?.MerchantHistory?.Count(m => m.MerchantId == history.MerchantId) ?? 1
            };
        }
    }
}