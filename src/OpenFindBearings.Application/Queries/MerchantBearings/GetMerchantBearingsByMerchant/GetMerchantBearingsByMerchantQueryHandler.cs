using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByMerchant
{
    /// <summary>
    /// 获取指定商家的所有关联查询处理器
    /// </summary>
    public class GetMerchantBearingsByMerchantQueryHandler : IRequestHandler<GetMerchantBearingsByMerchantQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingsByMerchantQueryHandler> _logger;

        public GetMerchantBearingsByMerchantQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingsByMerchantQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(
            GetMerchantBearingsByMerchantQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家轴承列表: MerchantId={MerchantId}, IsAuthenticated={IsAuthenticated}",
                request.MerchantId, request.IsAuthenticated);

            var result = await _merchantBearingRepository.GetMerchantBearingsPagedAsync(
                request.MerchantId, request.OnlyOnSale, request.PendingOnly,
                request.Page, request.PageSize, cancellationToken);

            return new PagedResult<MerchantBearingDto>
            {
                Items = result.Items.Select(mb => mb.ToDto(request.IsAuthenticated)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
