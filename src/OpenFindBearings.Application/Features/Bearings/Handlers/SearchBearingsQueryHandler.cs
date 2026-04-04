using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
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

            var items = result.Items.Select(b => new BearingDto
            {
                Id = b.Id,
                CurrentCode = b.CurrentCode,
                FormerCode = b.FormerCode,          
                Name = b.Name,
                Description = b.Description,
                BearingType = b.BearingType,
                InnerDiameter = b.Dimensions.InnerDiameter,
                OuterDiameter = b.Dimensions.OuterDiameter,
                Width = b.Dimensions.Width,
                Weight = b.Weight,
                BrandId = b.BrandId,
                BrandName = b.Brand?.Name ?? string.Empty,
                BearingTypeId = b.BearingTypeId,
                BearingTypeName = b.BearingType,
                ViewCount = b.ViewCount,
                FavoriteCount = b.FavoriteCount,
                OriginCountry = b.OriginCountry,
                Category = b.Category.ToString(),
                IsStandard = b.IsStandard           
            }).ToList();

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
