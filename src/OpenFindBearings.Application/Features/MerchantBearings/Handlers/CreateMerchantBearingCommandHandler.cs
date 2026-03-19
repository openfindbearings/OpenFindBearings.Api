using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 创建商家-轴承关联命令处理器
    /// </summary>
    public class CreateMerchantBearingCommandHandler : IRequestHandler<CreateMerchantBearingCommand, Guid>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<CreateMerchantBearingCommandHandler> _logger;

        public CreateMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            IBearingRepository bearingRepository,
            ILogger<CreateMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantRepository = merchantRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateMerchantBearingCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始创建商家-轴承关联: MerchantId={MerchantId}, BearingId={BearingId}",
                request.MerchantId, request.BearingId);

            // 验证商家是否存在
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 验证轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.BearingId}");
            }

            // 检查是否已存在关联
            var exists = await _merchantBearingRepository.ExistsAsync(request.MerchantId, request.BearingId, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"商家-轴承关联已存在: MerchantId={request.MerchantId}, BearingId={request.BearingId}");
            }

            // 创建关联
            var merchantBearing = new MerchantBearing(
                merchantId: request.MerchantId,
                bearingId: request.BearingId,
                priceDescription: request.PriceDescription,
                stockDescription: request.StockDescription);

            // 如果有最小起订量，设置
            if (!string.IsNullOrWhiteSpace(request.MinOrderDescription) || !string.IsNullOrWhiteSpace(request.Remarks))
            {
                merchantBearing.UpdateMarketInfo(
                    request.PriceDescription,
                    request.StockDescription,
                    request.MinOrderDescription,
                    request.Remarks);
            }

            // 新创建的关联需要提交审核
            merchantBearing.SubmitForApproval();

            await _merchantBearingRepository.AddAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家-轴承关联创建成功: Id={Id}, MerchantId={MerchantId}, BearingId={BearingId}",
                merchantBearing.Id, merchantBearing.MerchantId, merchantBearing.BearingId);

            return merchantBearing.Id;
        }
    }
}
