using MediatR;

namespace OpenFindBearings.Application.Commands.Merchants.DetachMerchant
{
    /// <summary>
    /// Admin 强制解除归属命令（v2.17.0）：平台侧对"管理员跑路/僵尸无主/违规商户"的处置入口，
    /// 与商户自助关店同走 release 分支（清场回公海，存在性交爬虫管线裁判），差别仅在
    /// 权限（merchant.detach）与通知文案（"由平台解除归属"）。
    /// </summary>
    /// <param name="MerchantId">目标商户</param>
    /// <param name="Reason">解除原因（审计与通知正文用）</param>
    public record DetachMerchantCommand(Guid MerchantId, string? Reason) : IRequest<DetachMerchantResult>;

    /// <summary>
    /// 解除结果：IsReleased=false 仅理论上存在（self/提名新建商户 Admin 应走删除而非 detach，
    /// 命令层对无公海数据可回的商户直接拒绝）。
    /// </summary>
    public record DetachMerchantResult(bool IsReleased, string MerchantName);
}
