using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Merchants.AdjustMerchantGrade
{
    /// <summary>
    /// 人工调整商家卡级命令（v2.18.0 卡级重构）：管理侧对卡级做人工干预——
    /// 用于申诉核实后的补偿、异常刷分核实后的降级等场景；留痕由请求级审计中间件记录。
    /// </summary>
    public record AdjustMerchantGradeCommand : IRequest, ICommand
    {
        /// <summary>目标商户标识</summary>
        public Guid MerchantId { get; init; }

        /// <summary>目标卡级（不得为 Unknown）</summary>
        public MerchantGrade Grade { get; init; }

        /// <summary>调整原因（复核/申诉依据）</summary>
        public string? Reason { get; init; }
    }
}
