using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Brands.GetBrand
{
    /// <summary>
    /// 获取单个品牌查询处理器
    /// </summary>
    public class GetBrandQueryHandler : IRequestHandler<GetBrandQuery, BrandDto?>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBrandQueryHandler> _logger;

        public GetBrandQueryHandler(
            IBrandRepository brandRepository,
            IBearingRepository bearingRepository,
            ILogger<GetBrandQueryHandler> logger)
        {
            _brandRepository = brandRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BrandDto?> Handle(GetBrandQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取品牌详情: Id={Id}", request.Id);

            var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);
            if (brand == null) return null;

            var bearingCountByBrand = await _bearingRepository.GetBearingCountByBrandAsync(cancellationToken);
            var bearingCount = bearingCountByBrand.GetValueOrDefault(brand.Id, 0);

            return brand.ToDto(bearingCount);
        }
    }
}
