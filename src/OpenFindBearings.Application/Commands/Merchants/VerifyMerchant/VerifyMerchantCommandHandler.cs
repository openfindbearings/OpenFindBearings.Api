using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.VerifyMerchant
{
    /// <summary>
    /// 认证商家命令处理器
    /// </summary>
    public class VerifyMerchantCommandHandler : IRequestHandler<VerifyMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<VerifyMerchantCommandHandler> _logger;

        public VerifyMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<VerifyMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantBearingRepository = merchantBearingRepository;
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

            merchant.Verify(request.VerifiedBy);
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 审核通过后清除该商户的爬虫关联数据
            await _merchantBearingRepository.DeleteByMerchantAndSourceAsync(
                merchant.Id, DataSourceType.Crawler, cancellationToken);

            _logger.LogInformation("商家认证成功, 已清除爬虫数据: {MerchantId}", request.Id);
        }
    }
}
