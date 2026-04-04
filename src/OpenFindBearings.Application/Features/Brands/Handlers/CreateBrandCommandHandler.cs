using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Brands.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Brands.Handlers
{
    /// <summary>
    /// 创建品牌命令处理器
    /// </summary>
    public class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, Guid>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<CreateBrandCommandHandler> _logger;

        public CreateBrandCommandHandler(
            IBrandRepository brandRepository,
            ILogger<CreateBrandCommandHandler> logger)
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("创建品牌: Code={Code}, Name={Name}, Level={Level}",
                request.Code, request.Name, request.Level);

            // 检查代码是否已存在
            var existing = await _brandRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException($"品牌代码已存在: {request.Code}");
            }

            var brand = new Brand(request.Code, request.Name, request.Level);
            brand.UpdateDetails(request.Country, request.LogoUrl);

            await _brandRepository.AddAsync(brand, cancellationToken);

            _logger.LogInformation("品牌创建成功: Id={Id}, Code={Code}", brand.Id, brand.Code);

            return brand.Id;
        }
    }
}
