using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Brands.GetAllBrands
{
    /// <summary>
    /// 获取所有品牌列表查询处理器
    /// </summary>
    public class GetAllBrandsQueryHandler : IRequestHandler<GetAllBrandsQuery, List<BrandDto>>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetAllBrandsQueryHandler> _logger;

        public GetAllBrandsQueryHandler(
            IBrandRepository brandRepository,
            IBearingRepository bearingRepository,
            ILogger<GetAllBrandsQueryHandler> logger)
        {
            _brandRepository = brandRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<List<BrandDto>> Handle(GetAllBrandsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取所有品牌列表");

            var brands = await _brandRepository.GetAllAsync(cancellationToken);
            var bearingCountByBrand = await _bearingRepository.GetBearingCountByBrandAsync(cancellationToken);

            return brands.Select(b => b.ToDto(bearingCountByBrand.GetValueOrDefault(b.Id, 0))).ToList();
        }
    }
}
