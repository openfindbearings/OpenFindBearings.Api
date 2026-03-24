using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Brands.DTOs;
using OpenFindBearings.Application.Features.Brands.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Brands.Handlers
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

            return new BrandDto
            {
                Id = brand.Id,
                Code = brand.Code,
                Name = brand.Name,
                Country = brand.Country,
                LogoUrl = brand.LogoUrl,
                Level = brand.Level.ToString(),
                BearingCount = bearingCount
            };
        }
    }
}
