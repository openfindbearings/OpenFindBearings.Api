using MediatR;

namespace OpenFindBearings.Application.Features.Corrections.Commands
{
    /// <summary>
    /// 审核通过纠错命令
    /// </summary>
    public record ApproveCorrectionCommand : IRequest
    {
        public Guid CorrectionId { get; init; }
        public string ReviewedBy { get; init; } = string.Empty; // AuthUserId
        public string? Comment { get; init; }
    }
}
