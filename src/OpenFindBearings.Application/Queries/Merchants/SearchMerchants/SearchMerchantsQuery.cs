using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.SearchMerchants
{
    /// <summary>
    /// 搜索商家查询
    /// </summary>
    public record SearchMerchantsQuery : IRequest<PagedResult<MerchantDto>>, IQuery
    {
        /// <summary>
        /// 关键词（搜索名称和公司名）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// 商家类型
        /// </summary>
        public MerchantType? Type { get; set; }

        /// <summary>
        /// 是否只显示认证商家
        /// </summary>
        public bool? VerifiedOnly { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
