using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 搜索轴承查询处理器
    /// </summary>
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
                PartNumber = request.PartNumber,
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
                SortBy = request.SortBy,
                SortOrder = request.SortOrder,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var bearings = await _bearingRepository.SearchAsync(searchParams, cancellationToken);

            // TODO: 需要实现 CountAsync 方法
            var totalCount = bearings.Count();

            var items = bearings.Select(b => new BearingDto
            {
                Id = b.Id,
                PartNumber = b.PartNumber,
                Name = b.Name,
                Description = b.Description,
                InnerDiameter = b.Dimensions.InnerDiameter,
                OuterDiameter = b.Dimensions.OuterDiameter,
                Width = b.Dimensions.Width,
                Weight = b.Weight,
                BrandId = b.BrandId,
                BrandName = b.Brand?.Name ?? string.Empty,
                BearingTypeId = b.BearingTypeId,
                BearingTypeName = b.BearingType?.Name ?? string.Empty,
                ViewCount = b.ViewCount,
                FavoriteCount = b.FavoriteCount,
                OriginCountry = b.OriginCountry,
                Category = b.Category.ToString()
            }).ToList();

            return new PagedResult<BearingDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
