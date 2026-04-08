using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Corrections.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Corrections.RejectCorrection
{
    /// <summary>
    /// 拒绝纠错命令处理器
    /// </summary>
    public class RejectCorrectionCommandHandler : IRequestHandler<RejectCorrectionCommand>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly ILogger<RejectCorrectionCommandHandler> _logger;

        public RejectCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            ILogger<RejectCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _logger = logger;
        }

        public async Task Handle(RejectCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("拒绝纠错: CorrectionId={CorrectionId}, Reviewer={ReviewerId}",
                request.CorrectionId, request.ReviewedBy);

            var correction = await _correctionRepository.GetByIdAsync(request.CorrectionId, cancellationToken);
            if (correction == null)
            {
                throw new InvalidOperationException($"纠错不存在: {request.CorrectionId}");
            }

            correction.Reject(request.ReviewedBy, request.Comment);
            await _correctionRepository.UpdateAsync(correction, cancellationToken);

            _logger.LogInformation("纠错已拒绝: CorrectionId={CorrectionId}", correction.Id);
        }
    }
}
