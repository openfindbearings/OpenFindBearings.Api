using MediatR;

namespace OpenFindBearings.Application.Features.Corrections.Commands
{
    /// <summary>
    /// 提交轴承纠错命令
    /// </summary>
    public record SubmitBearingCorrectionCommand : IRequest<Guid>
    {
        public Guid BearingId { get; set; }
        public string FieldName { get; init; } = string.Empty;
        public string SuggestedValue { get; init; } = string.Empty;
        public string? OriginalValue { get; init; }
        public string? Reason { get; init; }
        public string SubmittedBy { get; set; } = string.Empty; // AuthUserId
    }
}
