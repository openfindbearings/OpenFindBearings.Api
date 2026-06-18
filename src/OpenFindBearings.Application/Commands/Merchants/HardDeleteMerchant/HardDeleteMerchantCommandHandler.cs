using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.HardDeleteMerchant
{
    /// <summary>
    /// 彻底删除商家命令处理器（物理删除）
    /// </summary>
    public class HardDeleteMerchantCommandHandler : IRequestHandler<HardDeleteMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<HardDeleteMerchantCommandHandler> _logger;

        public HardDeleteMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<HardDeleteMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(HardDeleteMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始彻底删除商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            if (merchant.IsActive)
            {
                throw new InvalidOperationException("不能彻底删除激活状态的商家，请先软删除");
            }

            await _merchantRepository.RemoveAsync(merchant, cancellationToken);

            _logger.LogInformation("商家彻底删除成功: {MerchantId}", request.Id);
        }
    }
}
