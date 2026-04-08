using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Corrections.Commands
{
    /// <summary>
    /// 拒绝纠错命令
    /// </summary>
    public record RejectCorrectionCommand : IRequest, ICommand
    {
        /// <summary>
        /// 纠错ID
        /// </summary>
        public Guid CorrectionId { get; init; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        public Guid ReviewedBy { get; init; }

        /// <summary>
        /// 拒绝理由
        /// </summary>
        public string Comment { get; init; } = string.Empty;
    }
}
