using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Brands
{
    /// <summary>
    /// 根据代码获取品牌查询处理器
    /// </summary>
    public class GetBrandByCodeQueryHandler : IRequestHandler<GetBrandByCodeQuery, BrandDto?>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBrandByCodeQueryHandler> _logger;

        public GetBrandByCodeQueryHandler(
            IBrandRepository brandRepository,
            IBearingRepository bearingRepository,
            ILogger<GetBrandByCodeQueryHandler> logger)
        {
            _brandRepository = brandRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BrandDto?> Handle(GetBrandByCodeQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("根据代码获取品牌: Code={Code}", request.Code);

            var brand = await _brandRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (brand == null) return null;

            var bearingCountByBrand = await _bearingRepository.GetBearingCountByBrandAsync(cancellationToken);
            var bearingCount = bearingCountByBrand.GetValueOrDefault(brand.Id, 0);

            return brand.ToDto(bearingCount);
        }
    }
}
