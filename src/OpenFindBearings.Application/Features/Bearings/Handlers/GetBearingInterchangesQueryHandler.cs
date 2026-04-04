using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    public class GetBearingInterchangesQueryHandler : IRequestHandler<GetBearingInterchangesQuery, List<BearingDto>>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingInterchangesQueryHandler> _logger;

        public GetBearingInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            IBearingRepository bearingRepository,
            ILogger<GetBearingInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<List<BearingDto>> Handle(GetBearingInterchangesQuery request, CancellationToken cancellationToken)
        {
            var interchanges = await _interchangeRepository.GetBySourceBearingAsync(request.BearingId, cancellationToken);

            var result = new List<BearingDto>();

            foreach (var interchange in interchanges)
            {
                var bearing = await _bearingRepository.GetByIdAsync(interchange.TargetBearingId, cancellationToken);
                if (bearing != null)
                {
                    result.Add(new BearingDto
                    {
                        Id = bearing.Id,
                        CurrentCode = bearing.CurrentCode,
                        FormerCode = bearing.FormerCode,           // ✅ 新增
                        Name = bearing.Name,
                        Description = bearing.Description,
                        InnerDiameter = bearing.Dimensions.InnerDiameter,
                        OuterDiameter = bearing.Dimensions.OuterDiameter,
                        Width = bearing.Dimensions.Width,
                        BrandId = bearing.BrandId,
                        BrandName = bearing.Brand?.Name ?? string.Empty,
                        BearingTypeId = bearing.BearingTypeId,
                        BearingTypeName = bearing.BearingType,
                        ViewCount = bearing.ViewCount,
                        OriginCountry = bearing.OriginCountry,
                        Category = bearing.Category.ToString(),
                        IsStandard = bearing.IsStandard,           // ✅ 新增
                        Weight = bearing.Weight                    // ✅ 新增（可选）
                    });
                }
            }

            return result;
        }
    }
}
