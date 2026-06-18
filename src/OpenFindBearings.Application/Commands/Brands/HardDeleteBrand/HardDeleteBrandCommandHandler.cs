using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Brands.HardDeleteBrand
{
    /// <summary>
    /// 彻底删除品牌命令处理器（物理删除）
    /// </summary>
    public class HardDeleteBrandCommandHandler : IRequestHandler<HardDeleteBrandCommand>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<HardDeleteBrandCommandHandler> _logger;

        public HardDeleteBrandCommandHandler(
            IBrandRepository brandRepository,
            ILogger<HardDeleteBrandCommandHandler> logger)
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task Handle(HardDeleteBrandCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始彻底删除品牌: {BrandId}", request.Id);

            var brand = await _brandRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (brand == null)
            {
                throw new InvalidOperationException($"品牌不存在: {request.Id}");
            }

            if (brand.IsActive)
            {
                throw new InvalidOperationException("不能彻底删除激活状态的品牌，请先软删除");
            }

            await _brandRepository.RemoveAsync(brand, cancellationToken);

            _logger.LogInformation("品牌彻底删除成功: {BrandId}", request.Id);
        }
    }
}
