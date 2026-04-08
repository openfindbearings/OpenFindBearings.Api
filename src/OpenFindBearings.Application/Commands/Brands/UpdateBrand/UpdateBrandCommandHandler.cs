using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Brands.UpdateBrand
{
    /// <summary>
    /// 更新品牌命令处理器
    /// </summary>
    public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<UpdateBrandCommandHandler> _logger;

        public UpdateBrandCommandHandler(
            IBrandRepository brandRepository,
            ILogger<UpdateBrandCommandHandler> logger)
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新品牌: Id={Id}", request.Id);

            var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);
            if (brand == null)
            {
                throw new InvalidOperationException($"品牌不存在: {request.Id}");
            }

            // 更新品牌档次
            if (request.Level.HasValue)
            {
                brand.UpdateLevel(request.Level.Value);
            }

            // 更新详细信息
            brand.UpdateDetails(request.Country, request.LogoUrl);

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                brand.UpdateName(request.Name);
            }

            await _brandRepository.UpdateAsync(brand, cancellationToken);

            _logger.LogInformation("品牌更新成功: Id={Id}", brand.Id);
        }
    }
}
