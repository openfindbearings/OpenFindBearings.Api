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

            var merchantBearings = await _merchantBearingRepository.GetByMerchantAsync(request.MerchantId, cancellationToken);

            if (request.OnlyOnSale.HasValue)
            {
                merchantBearings = merchantBearings.Where(mb => mb.IsOnSale == request.OnlyOnSale.Value);
            }

            var totalCount = merchantBearings.Count();
            var items = merchantBearings
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(mb => mb.ToDto(request.IsAuthenticated))
                .ToList();

            return new PagedResult<MerchantBearingDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
