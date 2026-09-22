using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Corrections.GetCorrectableFields
{
    /// <summary>可纠错字段选项：Key 提交用字段键，Label 中文名，CurrentValue 当前值（供用户核对）</summary>
    public record CorrectionFieldOptionDto(string Key, string Label, string? CurrentValue);

    /// <summary>
    /// 可纠错字段查询（v2.14.0）：返回指定实体（轴承/商家）当前可提交纠错的字段清单，
    /// 含字段键、中文名与当前值——前端纠错表单据此渲染"选字段 → 显示当前值 → 填应改为"，
    /// 字段清单与审批端 Apply 分支保持同集合（只列采纳后能真正生效的字段，防"可提交但无效"）
    /// </summary>
    public record GetCorrectableFieldsQuery : IRequest<List<CorrectionFieldOptionDto>>, IQuery
    {
        /// <summary>目标类型 Bearing/Merchant</summary>
        public string TargetType { get; init; } = string.Empty;
        /// <summary>目标实体ID</summary>
        public Guid TargetId { get; init; }
    }
}
