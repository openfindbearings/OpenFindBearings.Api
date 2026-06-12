using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Brands.DeleteBrand
{
    /// <summary>
    /// 删除品牌命令处理器（软删除）
    /// </summary>
    public class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<DeleteBrandCommandHandler> _logger;

        public DeleteBrandCommandHandler(
            IBrandRepository brandRepository,
            ILogger<DeleteBrandCommandHandler> logger)
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始删除品牌: {BrandId}", request.Id);

            var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);
            if (brand == null)
            {
                throw new InvalidOperationException($"品牌不存在: {request.Id}");
            }

            brand.Deactivate();
            await _brandRepository.UpdateAsync(brand, cancellationToken);

            _logger.LogInformation("品牌删除成功: {BrandId}", request.Id);
        }
    }
}
