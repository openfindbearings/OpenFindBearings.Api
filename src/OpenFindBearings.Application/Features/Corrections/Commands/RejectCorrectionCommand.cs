using MediatR;

namespace OpenFindBearings.Application.Features.Corrections.Commands
{
    /// <summary>
    /// 拒绝纠错命令
    /// </summary>
    public record RejectCorrectionCommand : IRequest
    {
        public Guid CorrectionId { get; init; }
        public string ReviewedBy { get; init; } = string.Empty; // AuthUserId
        public string Comment { get; init; } = string.Empty;
    }
}
