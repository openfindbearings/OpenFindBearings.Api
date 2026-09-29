using StackExchange.Redis;

namespace OpenFindBearings.Api.Services
{
    /// <summary>
    /// 商户释放事件总线接口：关店/解除归属后向事件流发布"商户回公海"事件，
    /// 由 Sync 侧消费者拉取并唤醒 staging 刷新（替代原 API→Sync 同步 HTTP 出站）
    /// </summary>
    public interface IMerchantReleaseEventBus
    {
        /// <summary>
        /// 发布商户释放事件（fire-and-forget 语义：Redis 不可用仅告警，不阻塞、不回滚已完成的关店事务）
        /// </summary>
        /// <param name="merchantName">释放的商户名称（Sync 按名匹配 staging 行）</param>
        /// <param name="source">事件来源：detach=平台解除归属 / close=商家自助闭店</param>
        Task PublishMerchantReleasedAsync(string merchantName, string source, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 商户释放事件总线实现（Redis Stream）。
    /// 改动说明（v2.18.0 架构调整）：按"API 不直连 Sync"原则，唤醒通知从同步 HTTP 改为事件发布——
    /// API 只面向事件总线（不感知消费者），Sync 用消费者组 XREADGROUP 拉取，宕机期间消息滞留 PEL 自动补投。
    /// 已知缺口（诚实留痕）：关店事务提交与 XADD 之间进程崩溃会丢事件（Redis 无事务性 DB 联动）；
    /// 原型阶段接受，Sync 端点幂等可手动补调，真零丢失需再配 outbox 表中继。
    /// Redis 未启用（CacheSettings:EnableRedis=false 或连接失败）时 IConnectionMultiplexer 未注册，
    /// 此处降级为仅日志告警，行为与"未部署 Sync 的公开版"一致
    /// </summary>
    public class MerchantReleaseEventBus : IMerchantReleaseEventBus
    {
        /// <summary>商户释放事件流键（与 Sync 侧消费者约定一致，改动需两端同步）</summary>
        public const string ReleaseStreamKey = "merchant:release-events";

        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<MerchantReleaseEventBus> _logger;

        public MerchantReleaseEventBus(
            ILogger<MerchantReleaseEventBus> logger,
            IConnectionMultiplexer? redis = null)
        {
            _logger = logger;
            _redis = redis;
        }

        /// <inheritdoc/>
        public async Task PublishMerchantReleasedAsync(string merchantName, string source, CancellationToken cancellationToken = default)
        {
            if (_redis is null || !_redis.IsConnected)
            {
                // 改动说明：Redis 未启用时唤醒退化为"Sync 端点手动补调"，与原 best-effort 失败分支同语义
                _logger.LogWarning("Redis 未连接，商户释放事件未发布（可手动补调 Sync staging refresh）: Name={Name} Source={Source}",
                    merchantName, source);
                return;
            }

            try
            {
                var db = _redis.GetDatabase();
                // 近似 MAXLEN 10000 防流无界增长（消费者长期宕机时旧事件被裁剪，替代手动补调兜底）
                await db.StreamAddAsync(ReleaseStreamKey, new NameValueEntry[]
                {
                    new("merchantName", merchantName),
                    new("source", source),
                    new("occurredAt", DateTime.UtcNow.ToString("O"))
                }, maxLength: 10000, useApproximateMaxLength: true);
                _logger.LogInformation("商户释放事件已发布: Name={Name} Source={Source}", merchantName, source);
            }
            catch (Exception ex)
            {
                // fire-and-forget：发布失败不回滚关店（与 HTTP best-effort 同口径），Sync 端点幂等可手动补调
                _logger.LogWarning(ex, "商户释放事件发布失败（可手动补调 Sync staging refresh）: Name={Name}", merchantName);
            }
        }
    }
}
