namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 积分服务接口（v1.32.0 积分底座）：全系统唯一的积分写入口。
    /// 合规三纪律在实现层写死：无充值入口、无提现入口、无转账入口——
    /// 余额只能由规则表驱动的赚分动作产生
    /// </summary>
    public interface IPointsService
    {
        /// <summary>
        /// 发放积分（幂等+日上限+开关守卫，任何失败静默跳过返回 0，绝不抛出阻塞业务主流程）。
        /// 事件订阅者/中间件/命令处理器统一走此入口
        /// </summary>
        /// <param name="userId">获得积分的用户</param>
        /// <param name="grantType">动作类型（PointTransaction.Type* 常量，需规则表存在且启用）</param>
        /// <param name="bizId">幂等键（如 daily_login:{userId}:{yyyyMMdd}），重复直接跳过</param>
        /// <param name="remark">明细备注（可空）</param>
        /// <returns>实际发放分值（0=被幂等/上限/停用拦截）</returns>
        Task<int> GrantAsync(Guid userId, string grantType, string? bizId = null,
            string? remark = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发放一次性奖励（v1.34.0 防刷）：先向认领台账原子占坑（键=手机号/信用代码等
        /// 跨账号不变量），占到才 Grant。与 GrantAsync 的流水 bizId 幂等互补——
        /// 流水随注销清空后，台账仍在，同号重注册/同照重入驻刷不动大额一次性分。
        /// 同样吞异常返回 0，绝不阻塞业务主流程
        /// </summary>
        /// <param name="userId">获得积分的用户（当期操作人）</param>
        /// <param name="grantType">一次性动作类型</param>
        /// <param name="claimKey">台账幂等键（如 phone:138xxx:register、credit:91XXX:approved）</param>
        /// <param name="remark">明细备注（可空）</param>
        /// <returns>实际发放分值（0=台账已存在或规则停用/异常）</returns>
        Task<int> GrantOneTimeAsync(Guid userId, string grantType, string claimKey,
            string? remark = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 扣减积分（余额不足抛业务异常；扣分场景：商城兑换/寻货消耗，本期入口就位场景留白）
        /// </summary>
        /// <param name="userId">被扣分用户</param>
        /// <param name="sceneType">扣分场景标识</param>
        /// <param name="amount">扣减分值（正数）</param>
        /// <param name="bizId">幂等键（可空）</param>
        /// <param name="remark">明细备注（可空）</param>
        /// <returns>实际扣减分值</returns>
        Task<int> DeductAsync(Guid userId, string sceneType, int amount, string? bizId = null,
            string? remark = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 每日签到（阶梯分值+连续天数推进，bizId 幂等防同日重复）
        /// </summary>
        /// <param name="userId">签到用户</param>
        /// <returns>本次发放分值与连续天数；AlreadyCheckedIn=true 表示今日已签</returns>
        Task<CheckinResult> CheckinAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 签到结果
    /// </summary>
    /// <param name="Amount">本次发放分值（阶梯档）</param>
    /// <param name="ConsecutiveDays">本次签到后的连续天数</param>
    /// <param name="AlreadyCheckedIn">今日是否已签（true 时 Amount=0）</param>
    public record CheckinResult(int Amount, int ConsecutiveDays, bool AlreadyCheckedIn);
}
