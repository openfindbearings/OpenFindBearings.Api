using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Queries.Bearings.SearchBearings
{
    public class SearchBearingsQueryHandler : IRequestHandler<SearchBearingsQuery, PagedResult<BearingDto>>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<SearchBearingsQueryHandler> _logger;

        public SearchBearingsQueryHandler(
            IBearingRepository bearingRepository,
            ILogger<SearchBearingsQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<BearingDto>> Handle(SearchBearingsQuery request, CancellationToken cancellationToken)
        {
            var searchParams = new BearingSearchParams
            {
                CurrentCode = request.CurrentCode,
                FormerCode = request.FormerCode,
                Keyword = request.Keyword,
                MinInnerDiameter = request.MinInnerDiameter,
                MaxInnerDiameter = request.MaxInnerDiameter,
                MinOuterDiameter = request.MinOuterDiameter,
                MaxOuterDiameter = request.MaxOuterDiameter,
                MinWidth = request.MinWidth,
                MaxWidth = request.MaxWidth,
                OriginCountry = request.OriginCountry,
                Category = request.Category,
                BrandId = request.BrandId,
                BearingTypeId = request.BearingTypeId,
                IsStandard = request.IsStandard,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var result = await _bearingRepository.SearchAsync(searchParams, cancellationToken);

            var items = result.Items.Select(b => b.ToDto()).ToList();

            return new PagedResult<BearingDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
