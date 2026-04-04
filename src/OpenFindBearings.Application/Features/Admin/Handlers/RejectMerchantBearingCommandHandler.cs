using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    /// <summary>
    /// 拒绝商家产品命令处理器
    /// </summary>
    public class RejectMerchantBearingCommandHandler : IRequestHandler<RejectMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<RejectMerchantBearingCommandHandler> _logger;

        public RejectMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<RejectMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(RejectMerchantBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("拒绝商家产品: MerchantBearingId={MerchantBearingId}, Reviewer={ReviewerId}, Reason={Reason}",
                request.MerchantBearingId, request.ReviewedBy, request.Reason);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家产品不存在: {request.MerchantBearingId}");
            }

            merchantBearing.Reject(request.Reason);
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家产品已拒绝: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
