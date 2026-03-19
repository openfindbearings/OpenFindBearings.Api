using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 设置价格可见性命令处理器
    /// </summary>
    public class SetPriceVisibilityCommandHandler : IRequestHandler<SetPriceVisibilityCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<SetPriceVisibilityCommandHandler> _logger;

        public SetPriceVisibilityCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<SetPriceVisibilityCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(SetPriceVisibilityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("设置价格可见性: MerchantBearingId={MerchantBearingId}, Visibility={Visibility}",
                request.MerchantBearingId, request.Visibility);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家产品不存在: {request.MerchantBearingId}");
            }

            merchantBearing.SetPriceVisibility(request.Visibility);
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);
        }
    }
}
