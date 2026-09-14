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
                Status = merchant.Status.ToString(),
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
                // 改动说明：ToDetailDto 此前漏映射 LogoUrl/Website/UnifiedSocialCreditCode，
                //   导致商户详情页与"商户信息维护"页永远读不到 logo（恒空）与这两字段。补齐三字段映射。
                LogoUrl = merchant.LogoUrl,
                Website = merchant.Website,
                UnifiedSocialCreditCode = merchant.UnifiedSocialCreditCode
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
                // 改动说明：同 ToDetailDto 无参重载，补齐 LogoUrl/Website/UnifiedSocialCreditCode 映射
                LogoUrl = merchant.LogoUrl,
                Website = merchant.Website,
                UnifiedSocialCreditCode = merchant.UnifiedSocialCreditCode,
                // 改动说明：移除由 merchant.Staff（依赖已废弃的 User.MerchantId 关系）派生的员工列表；
                //   商户成员统一由成员表接口 GET /api/merchant/staff 提供，商家详情不再内嵌员工
                Products = products
            };
        }
    }
}