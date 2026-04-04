using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Brands.DTOs;
using OpenFindBearings.Application.Features.Brands.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Brands.Handlers
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

            return brands.Select(b => new BrandDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                Country = b.Country,
                LogoUrl = b.LogoUrl,
                Level = b.Level.ToString(),
                BearingCount = bearingCountByBrand.GetValueOrDefault(b.Id, 0)
            }).ToList();
        }
    }
}
