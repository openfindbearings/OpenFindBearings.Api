using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 下架产品命令处理器
    /// </summary>
    public class TakeOffShelfCommandHandler : IRequestHandler<TakeOffShelfCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<TakeOffShelfCommandHandler> _logger;

        public TakeOffShelfCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<TakeOffShelfCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(
            TakeOffShelfCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始下架产品: MerchantBearingId={MerchantBearingId}", request.MerchantBearingId);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            merchantBearing.TakeOffShelf();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("产品下架成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
