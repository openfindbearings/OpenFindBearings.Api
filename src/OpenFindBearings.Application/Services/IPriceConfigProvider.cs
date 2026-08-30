using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 价格配置提供器接口
    /// 职责：为应用层各处理器统一提供价格相关配置（默认可见性、议价标签、数值排序开关、提取正则）
    /// 说明：配置来源于 SystemConfigs 表，实现侧带进程内缓存，避免每条记录创建时反复查库
    /// </summary>
    public interface IPriceConfigProvider
    {
        /// <summary>
        /// 获取当前价格配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>价格配置；读取失败时返回各字段的代码默认值</returns>
        Task<PriceConfigDto> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 主动失效进程内缓存，使下一次读取重新查询数据库
        /// 说明：管理员修改价格配置后调用，避免等待 5 分钟 TTL 自然过期，
        ///       否则会出现 Admin 页面已显示新值而 API 侧仍按旧值写库的不一致窗口
        /// </summary>
        void Invalidate();
    }
}
