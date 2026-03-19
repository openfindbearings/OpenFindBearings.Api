using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    /// <summary>
    /// 审核通过商家产品命令处理器
    /// </summary>
    public class ApproveMerchantBearingCommandHandler : IRequestHandler<ApproveMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<ApproveMerchantBearingCommandHandler> _logger;

        public ApproveMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<ApproveMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveMerchantBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过商家产品: MerchantBearingId={MerchantBearingId}, Reviewer={ReviewerId}",
                request.MerchantBearingId, request.ReviewedBy);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家产品不存在: {request.MerchantBearingId}");
            }

            merchantBearing.Approve();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家产品审核通过成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
