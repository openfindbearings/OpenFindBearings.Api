using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 数据来源值对象
    /// 记录轴承数据的来源信息，用于判断是否允许爬虫数据覆盖
    /// 包含三个核心字段：SourceType、ImportedBy、ImportedAt
    /// </summary>
    public class DataSource : ValueObject
    {
        /// <summary>
        /// 来源类型
        /// </summary>
        public DataSourceType? SourceType { get; private set; }

        /// <summary>
        /// 录入人/系统（爬虫时存站点名称）
        /// </summary>
        public string? ImportedBy { get; private set; }

        /// <summary>
        /// 录入时间
        /// </summary>
        public DateTime ImportedAt { get; private set; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private DataSource() { }

        /// <summary>
        /// 创建手动录入的数据源
        /// </summary>
        public static DataSource FromManual(string? importedBy = null)
        {
            return new DataSource
            {
                SourceType = DataSourceType.Manual,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建爬虫数据源（站点名存到 ImportedBy）
        /// </summary>
        public static DataSource FromCrawler(string siteName)
        {
            return new DataSource
            {
                SourceType = DataSourceType.Crawler,
                ImportedBy = siteName,
                ImportedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建文件导入的数据源
        /// </summary>
        public static DataSource FromFileImport(string? importedBy = null)
        {
            return new DataSource
            {
                SourceType = DataSourceType.FileImport,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建API对接的数据源
        /// </summary>
        public static DataSource FromApi(string? importedBy = "ApiSync")
        {
            return new DataSource
            {
                SourceType = DataSourceType.Api,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 是否为爬虫数据（可以被覆盖）
        /// </summary>
        public bool IsCrawler => SourceType == DataSourceType.Crawler;

        /// <summary>
        /// 获取来源摘要
        /// </summary>
        public string GetSummary()
        {
            var typeName = SourceType?.ToString() ?? "未知";
            var sourceName = ImportedBy ?? "未知";
            return sourceName != null ? $"{typeName} - {sourceName}" : typeName;
        }

        /// <summary>
        /// 返回字符串表示
        /// </summary>
        public override string ToString() => GetSummary();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return SourceType ?? DataSourceType.Manual;
            yield return ImportedBy ?? string.Empty;
        }
    }
}