namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>新建替代关系请求（平台，v2.16.0）</summary>
    public record CreateInterchangeRequest(
        Guid SourceBearingId,
        Guid TargetBearingId,
        string? InterchangeType,
        int? Confidence,
        string? Source,
        string? Remarks,
        bool? IsBidirectional);

    /// <summary>编辑替代关系请求（平台，v2.16.0）</summary>
    public record UpdateInterchangeRequest(
        string? InterchangeType,
        int? Confidence,
        string? Source,
        string? Remarks,
        bool? IsBidirectional);

    // 改动说明（v2.17.0 替代品字典化）：删除 RejectInterchangeRequest——驳回端点随商家候选流一并取消
}