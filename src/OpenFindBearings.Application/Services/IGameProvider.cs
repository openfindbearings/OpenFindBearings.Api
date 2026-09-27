using System.Text.Json;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 游戏中心插件接口（v2.10.1 方案 A：进程内插件、微服务形状）。
    /// 每个游戏一个实现类注册进 DI，统一端点 /api/games/{key}/* 按 Key 路由——
    /// 加游戏=加一个 Provider 类，不动主链路；将来要拆独立服务时，
    /// 新服务实现同一份 HTTP 契约即可整组平移（发分仍回主 API 走 PointsService）。
    /// 资源纪律：实现必须轻量（每局最多一次读库+一次发分），禁止在出题/结算里做重查询。
    /// </summary>
    public interface IGameProvider
    {
        /// <summary>游戏唯一键（URL 段，如 linkup）</summary>
        string Key { get; }

        /// <summary>游戏显示名（日志与错误提示用）</summary>
        string DisplayName { get; }

        /// <summary>
        /// 出一局题板（客户端渲染数据）。size=规模参数（连连看=图片对数），实现自行钳制上下限。
        /// </summary>
        Task<object?> GetBoardAsync(Guid userId, int? size, CancellationToken cancellationToken = default);

        /// <summary>
        /// 结算一局结果并返回实发积分。payload=客户端上报体（各游戏自定义，如 {gameId}）；
        /// 发分实现必须走 PointsService.GrantAsync 幂等 bizId + 规则表日限，返回 0 表示今日额度满或重复提交。
        /// </summary>
        Task<int> ReportResultAsync(Guid userId, JsonElement payload, CancellationToken cancellationToken = default);
    }
}
