using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 系统配置仓储实现
    /// 职责：提供系统配置项的按键读取与更新
    /// </summary>
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemConfigRepository> _logger;

        /// <summary>
        /// 创建系统配置仓储
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="logger">日志记录器，用于记录配置值解析失败</param>
        public SystemConfigRepository(ApplicationDbContext context, ILogger<SystemConfigRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfigs.ToListAsync(cancellationToken);
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        }

        /// <summary>
        /// 获取配置值（泛型）
        /// </summary>
        public async Task<T?> GetValueAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default)
        {
            var config = await GetByKeyAsync(key, cancellationToken);
            if (config == null || string.IsNullOrEmpty(config.Value))
                return defaultValue;

            try
            {
                // 改动说明：bool 单独走宽松解析。Convert.ChangeType 只接受 "True"/"False"，
                //           "1"/"0"/"yes"/"no" 这类常见输入会抛异常并回退默认值，
                //           其中 "0" 回退为 true 与管理员"关闭"的意图完全相反，属静默反向生效
                if (typeof(T) == typeof(bool) && TryParseBool(config.Value, out var boolValue))
                {
                    return (T)(object)boolValue;
                }

                return (T)Convert.ChangeType(config.Value, typeof(T));
            }
            catch (Exception ex)
            {
                // 改动说明：原实现裸 catch 且无任何日志，配置写错值时静默回退默认值。
                //           运维在后台看到"保存成功"，实际行为可能相反且毫无痕迹，难以排查
                _logger.LogWarning(ex,
                    "系统配置值解析失败，已回退默认值: Key={Key}, RawValue={RawValue}, TargetType={TargetType}",
                    key, config.Value, typeof(T).Name);
                return defaultValue;
            }
        }

        /// <summary>
        /// 宽松解析布尔值，兼容 true/false、1/0、yes/no、on/off 等常见写法
        /// </summary>
        /// <param name="raw">原始配置值文本</param>
        /// <param name="result">解析结果</param>
        /// <returns>是否解析成功；false 表示无法识别，调用方应继续走默认解析逻辑</returns>
        private static bool TryParseBool(string raw, out bool result)
        {
            switch (raw.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                case "on":
                    result = true;
                    return true;

                case "false":
                case "0":
                case "no":
                case "n":
                case "off":
                    result = false;
                    return true;

                default:
                    result = false;
                    return false;
            }
        }

        public async Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default)
        {
            _context.SystemConfigs.Update(config);
            
        }
    }
}
