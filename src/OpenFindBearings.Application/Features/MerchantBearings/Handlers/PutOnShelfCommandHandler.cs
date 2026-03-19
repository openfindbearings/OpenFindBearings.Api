using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 上架产品命令处理器
    /// </summary>
    public class PutOnShelfCommandHandler : IRequestHandler<PutOnShelfCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<PutOnShelfCommandHandler> _logger;

        public PutOnShelfCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<PutOnShelfCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(
            PutOnShelfCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始上架产品: MerchantBearingId={MerchantBearingId}", request.MerchantBearingId);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            merchantBearing.PutOnShelf();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("产品上架成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
