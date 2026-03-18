using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 拒绝纠错命令处理器
    /// </summary>
    public class RejectCorrectionCommandHandler : IRequestHandler<RejectCorrectionCommand>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RejectCorrectionCommandHandler> _logger;

        public RejectCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            ILogger<RejectCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(RejectCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("拒绝纠错: CorrectionId={CorrectionId}, Reviewer={AuthUserId}",
                request.CorrectionId, request.ReviewedBy);

            var correction = await _correctionRepository.GetByIdAsync(request.CorrectionId, cancellationToken);
            if (correction == null)
            {
                throw new InvalidOperationException($"纠错不存在: {request.CorrectionId}");
            }

            var reviewer = await _userRepository.GetByAuthUserIdAsync(request.ReviewedBy, cancellationToken);
            if (reviewer == null)
            {
                throw new InvalidOperationException($"审核人不存在: {request.ReviewedBy}");
            }

            correction.Reject(reviewer.Id, request.Comment);
            await _correctionRepository.UpdateAsync(correction, cancellationToken);

            _logger.LogInformation("纠错已拒绝: CorrectionId={CorrectionId}", correction.Id);
        }
    }
}
