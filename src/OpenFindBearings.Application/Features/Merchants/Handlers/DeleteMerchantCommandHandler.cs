using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 删除商家命令处理器
    /// </summary>
    public class DeleteMerchantCommandHandler : IRequestHandler<DeleteMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<DeleteMerchantCommandHandler> _logger;

        public DeleteMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<DeleteMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始删除商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            merchant.Deactivate(); // 软删除
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家删除成功: {MerchantId}", request.Id);
        }
    }
}
