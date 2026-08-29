namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 可信度阈值配置 DTO，供 Sync 运行时拉取，替代其 appsettings 默认值
    /// </summary>
    public class ReliabilityConfigDto
    {
        /// <summary>自动同步阈值：可信度 ≥ 此值直接入库</summary>
        public int AutoSyncThreshold { get; set; } = 85;

        /// <summary>人工审核阈值：可信度 ≥ 此值且低于 AutoSyncThreshold 则进入待审核</summary>
        public int ReviewThreshold { get; set; } = 60;

        /// <summary>来源默认基础分：未在 SourceScores 中指定的来源使用此值</summary>
        public int DefaultSourceScore { get; set; } = 80;
    }
}
