using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Enums.Extensions;

namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 数据来源值对象
    /// 记录轴承数据的来源信息，用于数据追溯和质量评估
    /// </summary>
    public class DataSource : ValueObject
    {
        /// <summary>
        /// 来源类型
        /// </summary>
        public DataSourceType SourceType { get; private set; }

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
        /// 数据录入人/系统
        /// </summary>
        public string? ImportedBy { get; private set; }

        /// <summary>
        /// 录入时间
        /// </summary>
        public DateTime ImportedAt { get; private set; }

        /// <summary>
        /// 数据可信度（0-100）
        /// </summary>
        public int? ReliabilityScore { get; private set; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private DataSource() { }

        /// <summary>
        /// 创建手动录入的数据源
        /// </summary>
        public static DataSource FromManual(
            string? importedBy = null,
            string? sourceDetail = null,
            int? reliabilityScore = 90)
        {
            return new DataSource
            {
                SourceType = DataSourceType.Manual,
                SourceDetail = sourceDetail ?? "手动录入",
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                ReliabilityScore = reliabilityScore ?? 90
            };
        }

        /// <summary>
        /// 创建爬虫数据源
        /// </summary>
        public static DataSource FromCrawler(
            CrawlerSourceSite site,
            string sourceUrl,
            string? sourceId = null,
            string? importedBy = "CrawlerSystem",
            int? reliabilityScore = null)
        {
            return new DataSource
            {
                SourceType = DataSourceType.Crawler,
                CrawlerSite = site,
                SourceUrl = sourceUrl,
                SourceId = sourceId,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                ReliabilityScore = reliabilityScore ?? site.GetReliabilityScore(),
                SourceDetail = site.GetDisplayName()
            };
        }

        /// <summary>
        /// 创建文件导入的数据源
        /// </summary>
        public static DataSource FromFileImport(
            string fileName,
            string? importedBy = null,
            int? reliabilityScore = 85)
        {
            return new DataSource
            {
                SourceType = DataSourceType.FileImport,
                SourceDetail = fileName,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                ReliabilityScore = reliabilityScore ?? 85
            };
        }

        /// <summary>
        /// 创建API对接的数据源
        /// </summary>
        public static DataSource FromApi(
            string apiName,
            string? sourceId = null,
            string? importedBy = "ApiSync",
            int? reliabilityScore = 90)
        {
            return new DataSource
            {
                SourceType = DataSourceType.Api,
                SourceDetail = apiName,
                SourceId = sourceId,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                ReliabilityScore = reliabilityScore ?? 90
            };
        }

        /// <summary>
        /// 创建OCR/截图识别的数据源
        /// </summary>
        public static DataSource FromOcr(
            string imageSource,
            string? sourceId = null,
            string? importedBy = null,
            int? reliabilityScore = 80)
        {
            return new DataSource
            {
                SourceType = DataSourceType.OcrExtract,
                SourceDetail = imageSource,
                SourceId = sourceId,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                ReliabilityScore = reliabilityScore ?? 80
            };
        }

        /// <summary>
        /// 创建用户提交的数据源
        /// </summary>
        public static DataSource FromUserSubmission(
            string userId,
            string? sourceDetail = null,
            int? reliabilityScore = 70)
        {
            return new DataSource
            {
                SourceType = DataSourceType.UserSubmitted,
                SourceDetail = sourceDetail ?? "用户提交",
                ImportedBy = userId,
                ImportedAt = DateTime.UtcNow,
                ReliabilityScore = reliabilityScore ?? 70
            };
        }

        /// <summary>
        /// 是否为官方数据源
        /// </summary>
        public bool IsOfficialSource =>
            SourceType == DataSourceType.Crawler &&
            CrawlerSite.HasValue &&
            CrawlerSite.Value.IsOfficial();

        /// <summary>
        /// 获取来源摘要
        /// </summary>
        public string GetSummary()
        {
            if (SourceType == DataSourceType.Crawler && CrawlerSite.HasValue)
            {
                return $"爬虫采集 - {CrawlerSite.Value.GetDisplayName()} - {SourceUrl}";
            }

            return $"{SourceType} - {SourceDetail}";
        }

        /// <summary>
        /// 获取用于相等性比较的组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return SourceType;
            yield return CrawlerSite ?? CrawlerSourceSite.Unknown;
            yield return SourceUrl ?? string.Empty;
            yield return SourceId ?? string.Empty;
        }

        /// <summary>
        /// 返回字符串表示
        /// </summary>
        public override string ToString() => GetSummary();
    }
}
