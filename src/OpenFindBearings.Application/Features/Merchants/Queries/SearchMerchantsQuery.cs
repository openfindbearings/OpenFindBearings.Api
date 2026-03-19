using MediatR;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Merchants.Queries
{
    /// <summary>
    /// 搜索商家查询
    /// </summary>
    public class SearchMerchantsQuery : IRequest<PagedResult<MerchantDto>>
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
