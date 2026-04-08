using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Corrections.SubmitMerchantCorrection
{
    /// <summary>
    /// 提交商家纠错命令
    /// </summary>
    public record SubmitMerchantCorrectionCommand : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; init; } = string.Empty;

        /// <summary>
        /// 建议值
        /// </summary>
        public string SuggestedValue { get; init; } = string.Empty;

        /// <summary>
        /// 原始值
        /// </summary>
        public string? OriginalValue { get; init; }

        /// <summary>
        /// 纠错理由
        /// </summary>
        public string? Reason { get; init; }

        /// <summary>
        /// 提交人ID
        /// </summary>
        public Guid SubmittedBy { get; init; }
    }
}
