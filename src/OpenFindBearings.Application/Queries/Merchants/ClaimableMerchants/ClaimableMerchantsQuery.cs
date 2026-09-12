using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.ClaimableMerchants
{
    /// <summary>
    /// 可认领爬虫商家搜索查询
    /// </summary>
    public record ClaimableMerchantsQuery : IRequest<OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>>
    {
        /// <summary>
        /// 搜索关键词（商户名称/公司全称）
        /// </summary>
        public string? Keyword { get; init; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; init; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; init; } = 20;
    }

    /// <summary>
    /// 可认领爬虫商家搜索查询处理器
    /// 返回爬虫来源且未被认领（无在职成员）的商家
    /// </summary>
    public class ClaimableMerchantsQueryHandler : IRequestHandler<ClaimableMerchantsQuery, OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<ClaimableMerchantsQueryHandler> _logger;

        public ClaimableMerchantsQueryHandler(
            IMerchantRepository merchantRepository,
            ILogger<ClaimableMerchantsQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>> Handle(
            ClaimableMerchantsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("认领搜索: Keyword={Keyword}", request.Keyword);

            var result = await _merchantRepository.GetClaimableAsync(
                request.Keyword, request.Page, request.PageSize, cancellationToken);

            return new OpenFindBearings.Domain.Repositories.PagedResult<ClaimableMerchantDto>
            {
                Items = result.Items.Select(m => new ClaimableMerchantDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    CompanyName = m.CompanyName,
                    Type = m.GetMerchantTypeDisplayName()
                }).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
