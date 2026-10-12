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

    /// <summary>驳回替代候选请求（平台，附理由，v2.16.0）</summary>
    public record RejectInterchangeRequest(string? Reason);
}