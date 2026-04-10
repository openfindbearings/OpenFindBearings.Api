using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Aggregates;

namespace OpenFindBearings.Application.Extensions
{
    public static class MerchantExtensions
    {
        public static MerchantDto ToPublicDto(this Merchant merchant)
        {
            return new MerchantDto
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
                ProductCount = merchant.ProductCount,
                LogoUrl = merchant.LogoUrl
            };
        }

        public static MerchantDetailDto ToDetailDto(this Merchant merchant)
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
                VerifiedAt = merchant.VerifiedAt
            };
        }

        public static MerchantDetailDto ToDetailDto(this Merchant merchant, List<MerchantBearingDto> products, bool isAuthenticated)
        {
            return new MerchantDetailDto
            {
                Id = merchant.Id,
                Name = merchant.Name,
                CompanyName = merchant.CompanyName,
                Type = merchant.Type.ToString(),
                // ========== 敏感信息控制 ==========
                ContactPerson = isAuthenticated ? merchant.Contact?.ContactPerson : null,
                Phone = isAuthenticated ? merchant.Contact?.Phone : null,
                Mobile = isAuthenticated ? merchant.Contact?.Mobile : null,
                Email = isAuthenticated ? merchant.Contact?.Email : null,
                Address = isAuthenticated ? merchant.Contact?.Address : null,
                IsVerified = merchant.IsVerified,
                Grade = merchant.Grade.ToString(),
                FollowerCount = merchant.FollowerCount,
                ProductCount = products.Count,
                Description = merchant.Description,
                BusinessScope = merchant.BusinessScope,
                VerifiedAt = merchant.VerifiedAt,
                Staff = !isAuthenticated ? [] :merchant.Staff?.Select(s => new MerchantStaffDto
                {
                    Id = s.Id,
                    Nickname = s.Nickname ?? string.Empty,
                    Avatar = s.Avatar,
                    Role = "员工"
                }).ToList() ?? [],
                Products = products
            };
        }
    }
}