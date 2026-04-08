using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Corrections.Commands
{
    /// <summary>
    /// 审核通过纠错命令
    /// </summary>
    public record ApproveCorrectionCommand : IRequest, ICommand
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
        /// 审核意见
        /// </summary>
        public string? Comment { get; init; }
    }
}
