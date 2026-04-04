using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{ /// <summary>
  /// 更新商家-轴承关联命令处理器
  /// </summary>
    public class UpdateMerchantBearingCommandHandler : IRequestHandler<UpdateMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<UpdateMerchantBearingCommandHandler> _logger;

        public UpdateMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<UpdateMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(
            UpdateMerchantBearingCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始更新商家-轴承关联: Id={Id}", request.Id);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.Id}");
            }

            // 检查是否有变化
            bool hasChanges = false;

            if (request.PriceDescription != merchantBearing.PriceDescription ||
                request.StockDescription != merchantBearing.StockDescription ||
                request.MinOrderDescription != merchantBearing.MinOrderDescription ||
                request.Remarks != merchantBearing.Remarks)
            {
                hasChanges = true;
            }

            // 更新市场信息
            merchantBearing.UpdateMarketInfo(
                request.PriceDescription,
                request.StockDescription,
                request.MinOrderDescription,
                request.Remarks);

            // 如果有变化，需要重新提交审核
            if (hasChanges)
            {
                merchantBearing.SubmitForApproval();
            }

            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家-轴承关联更新成功: Id={Id}", merchantBearing.Id);
        }
    }
}
