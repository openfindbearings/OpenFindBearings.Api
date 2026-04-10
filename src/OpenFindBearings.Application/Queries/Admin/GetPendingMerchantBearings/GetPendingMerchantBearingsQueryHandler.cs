using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingMerchantBearings
{
    public class GetPendingMerchantBearingsQueryHandler : IRequestHandler<GetPendingMerchantBearingsQuery, PagedResult<PendingMerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetPendingMerchantBearingsQueryHandler> _logger;

        public GetPendingMerchantBearingsQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            IBearingRepository bearingRepository,
            ILogger<GetPendingMerchantBearingsQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantRepository = merchantRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<PendingMerchantBearingDto>> Handle(
            GetPendingMerchantBearingsQuery request,
            CancellationToken cancellationToken)
        {
            var pendingItems = await _merchantBearingRepository.GetPendingApprovalAsync(cancellationToken);

            var items = pendingItems
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var result = new List<PendingMerchantBearingDto>();

            foreach (var item in items)
            {
                var merchant = item.Merchant ?? await _merchantRepository.GetByIdAsync(item.MerchantId, cancellationToken);
                var bearing = item.Bearing ?? await _bearingRepository.GetByIdAsync(item.BearingId, cancellationToken);

                result.Add(new PendingMerchantBearingDto
                {
                    Id = item.Id,
                    Merchant = merchant?.ToPublicDto() ?? new MerchantDto(),
                    Bearing = bearing?.ToDto() ?? new BearingDto(),
                    PriceDescription = item.PriceDescription,
                    StockDescription = item.StockDescription,
                    MinOrderDescription = item.MinOrderDescription,
                    Remarks = item.Remarks,
                    CreatedAt = item.CreatedAt,
                    SubmitterName = "待实现"
                });
            }

            return new PagedResult<PendingMerchantBearingDto>
            {
                Items = result,
                TotalCount = pendingItems.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
