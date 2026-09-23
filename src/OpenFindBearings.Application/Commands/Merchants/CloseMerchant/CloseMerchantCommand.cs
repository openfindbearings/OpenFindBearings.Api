using MediatR;

namespace OpenFindBearings.Application.Commands.Merchants.CloseMerchant
{
    /// <summary>
    /// 商户自助关店命令（v2.17.0）：任一在职管理员发起，无需全体同意（对标 GitHub org 删除/飞书卸任，
    /// 关店损失可逆——公海开放，其余管理员收通知后可重新认领）。
    /// 通道二分：self/提名新建→删除分支（无公海数据可回，整删）；claim/提名已有→release 分支
    /// （解除归属回公海，存在性交爬虫管线裁判）。
    /// </summary>
    public record CloseMerchantCommand(Guid MerchantId, Guid OperatorUserId) : IRequest<CloseMerchantResult>;

    /// <summary>
    /// 关店执行结果：供端点层决定后续动作（release 分支需再调 Sync refresh 唤醒数据刷新；
    /// 删除分支无 staging 行不刷）。
    /// </summary>
    /// <param name="IsReleased">true=回公海（release），false=已整删（self/提名新建）</param>
    /// <param name="MerchantName">商户名（审计与日志用）</param>
    public record CloseMerchantResult(bool IsReleased, string MerchantName);
}
