namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 信息来源类型
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// 手动录入
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Excel/CSV 批量导入
        /// </summary>
        FileImport = 1,

        /// <summary>
        /// API 对接
        /// </summary>
        Api = 2,

        /// <summary>
        /// 爬虫采集
        /// </summary>
        Crawler = 3,

        /// <summary>
        /// 用户提交/众包
        /// </summary>
        UserSubmitted = 4,

        /// <summary>
        /// 系统初始化/种子数据
        /// </summary>
        SeedData = 5,

        /// <summary>
        /// OCR/截图识别
        /// </summary>
        OcrExtract = 6
    }
}
