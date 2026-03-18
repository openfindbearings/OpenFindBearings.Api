using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 认证商家命令处理器
    /// </summary>
    public class VerifyMerchantCommandHandler : IRequestHandler<VerifyMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<VerifyMerchantCommandHandler> _logger;

        public VerifyMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<VerifyMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(VerifyMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始认证商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            merchant.Verify();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家认证成功: {MerchantId}", request.Id);
        }
    }
}
