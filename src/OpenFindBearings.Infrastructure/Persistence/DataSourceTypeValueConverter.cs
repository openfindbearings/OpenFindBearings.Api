using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence
{
    /// <summary>
    /// DataSource.SourceType 的 EF 值转换器（历史库兼容）。
    /// 该列以枚举名字符串落库，而"互联网来源"的历史取值是 "Crawler"（枚举成员此后更名为 Internet）。
    /// 本转换器让库中的 "Crawler" 与 Internet 双向等价：
    ///   读取："Crawler" 解析为 DataSourceType.Internet，其余按枚举名解析；
    ///   写入：Internet 仍写回 "Crawler"，沿用库中原有取值。
    /// 目的：枚举更名后无需改动数据库、无需数据迁移，即可让读写正常并保持 SQL 过滤（== Internet 翻译为 = 'Crawler'）与历史行一致。
    /// </summary>
    public static class DataSourceTypeValueConverter
    {
        /// <summary>
        /// 转换器单例：供各实体 owned DataSource 的 SourceType 列（DB 列 DataSourceType）统一配置。
        /// </summary>
        public static readonly ValueConverter<DataSourceType, string> Instance =
            new ValueConverter<DataSourceType, string>(
                v => v == DataSourceType.Internet ? "Crawler" : v.ToString(),
                v => v == "Crawler" ? DataSourceType.Internet : Enum.Parse<DataSourceType>(v));
    }
}