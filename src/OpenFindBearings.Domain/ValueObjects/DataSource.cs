using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Enums.Extensions;

namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 数据来源值对象
    /// 记录轴承数据的来源信息，用于判断是否允许爬虫数据覆盖
    /// 可信度计算和同步决策由 Sync 服务维护
    /// </summary>
    public class DataSource : ValueObject
    {
        /// <summary>
        /// 来源类型
        /// </summary>
        public DataSourceType? SourceType { get; private set; }

        /// <summary>
        /// 爬虫来源网站（仅当 SourceType = Crawler 时有效）
        /// </summary>
        public CrawlerSourceSite? CrawlerSite { get; private set; }

        /// <summary>
        /// 具体来源URL（爬虫时记录原始URL）
        /// </summary>
        public string? SourceUrl { get; private set; }

        /// <summary>
        /// 具体来源描述
        /// </summary>
        public string? SourceDetail { get; private set; }

        /// <summary>
        /// 原始数据标识
        /// </summary>
        public string? SourceId { get; private set; }

        /// <summary>
        /// 录入人/系统
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
        public static DataSource FromManual(
            string? importedBy = null,
            string? sourceDetail = null)
        {
            return new DataSource
            {
                SourceType = DataSourceType.Manual,
                SourceDetail = sourceDetail ?? "手动录入",
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建爬虫数据源
        /// </summary>
        public static DataSource FromCrawler(
            CrawlerSourceSite site,
            string sourceUrl,
            string? sourceId = null,
            string? importedBy = "CrawlerSystem")
        {
            return new DataSource
            {
                SourceType = DataSourceType.Crawler,
                CrawlerSite = site,
                SourceUrl = sourceUrl,
                SourceId = sourceId,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                SourceDetail = site.GetDisplayName()
            };
        }

        /// <summary>
        /// 创建文件导入的数据源
        /// </summary>
        public static DataSource FromFileImport(
            string fileName,
            string? importedBy = null)
        {
            return new DataSource
            {
                SourceType = DataSourceType.FileImport,
                SourceDetail = fileName,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建API对接的数据源
        /// </summary>
        public static DataSource FromApi(
            string apiName,
            string? sourceId = null,
            string? importedBy = "ApiSync")
        {
            return new DataSource
            {
                SourceType = DataSourceType.Api,
                SourceDetail = apiName,
                SourceId = sourceId,
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
            if (SourceType == DataSourceType.Crawler && CrawlerSite.HasValue)
            {
                return $"爬虫采集 - {CrawlerSite.Value.GetDisplayName()}";
            }

            return $"{SourceType} - {SourceDetail}";
        }

        /// <summary>
        /// 返回字符串表示
        /// </summary>
        public override string ToString() => GetSummary();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return SourceType!;
            yield return CrawlerSite ?? CrawlerSourceSite.Unknown;
            yield return SourceUrl ?? string.Empty;
            yield return SourceId ?? string.Empty;
        }
    }
}
