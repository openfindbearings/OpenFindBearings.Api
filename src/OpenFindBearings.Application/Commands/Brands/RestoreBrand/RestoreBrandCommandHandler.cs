using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Brands.RestoreBrand
{
    /// <summary>
    /// 恢复已删除品牌命令处理器
    /// </summary>
    public class RestoreBrandCommandHandler : IRequestHandler<RestoreBrandCommand>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<RestoreBrandCommandHandler> _logger;

        public RestoreBrandCommandHandler(
            IBrandRepository brandRepository,
            ILogger<RestoreBrandCommandHandler> logger)
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task Handle(RestoreBrandCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始恢复品牌: {BrandId}", request.Id);

            var brand = await _brandRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (brand == null)
            {
                throw new InvalidOperationException($"品牌不存在: {request.Id}");
            }

            brand.Activate();
            await _brandRepository.UpdateAsync(brand, cancellationToken);

            _logger.LogInformation("品牌恢复成功: {BrandId}", request.Id);
        }
    }
}
