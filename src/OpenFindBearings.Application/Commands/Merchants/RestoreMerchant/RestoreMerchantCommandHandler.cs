using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.RestoreMerchant
{
    /// <summary>
    /// 恢复已删除商家命令处理器
    /// </summary>
    public class RestoreMerchantCommandHandler : IRequestHandler<RestoreMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<RestoreMerchantCommandHandler> _logger;

        public RestoreMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<RestoreMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(RestoreMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始恢复商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            merchant.Activate();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家恢复成功: {MerchantId}", request.Id);
        }
    }
}
