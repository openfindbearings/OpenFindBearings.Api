namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 爬虫来源网站
    /// </summary>
    public enum CrawlerSourceSite
    {
        /// <summary>
        /// 未知来源
        /// </summary>
        Unknown = 0,

        // ============ 国际品牌官网 (1-9) ============

        /// <summary>
        /// SKF 官网
        /// </summary>
        SkfOfficial = 1,

        /// <summary>
        /// FAG/INA 官网
        /// </summary>
        FagOfficial = 2,

        /// <summary>
        /// NSK 官网
        /// </summary>
        NskOfficial = 3,

        /// <summary>
        /// NTN 官网
        /// </summary>
        NtnOfficial = 4,

        /// <summary>
        /// TIMKEN 官网
        /// </summary>
        TimkenOfficial = 5,

        // ============ 行业协会/权威机构 (10-19) ============

        /// <summary>
        /// 中国轴承工业协会 (www.cbia.com.cn)
        /// 国家级行业协会，发布行业标准、统计数据、企业名录等
        /// </summary>
        ChinaBearingIndustryAssociation = 10,

        // ============ 电商平台 (20-29) ============

        /// <summary>
        /// 阿里巴巴/1688
        /// </summary>
        Alibaba = 20,

        /// <summary>
        /// 淘宝/天猫
        /// </summary>
        Taobao = 21,

        /// <summary>
        /// 京东
        /// </summary>
        JD = 22,

        // ============ 行业垂直网站 (30-39) ============

        /// <summary>
        /// 佰联轴承网 (www.bearing.cn)
        /// 国内知名轴承行业门户，提供型号查询、企业信息、行情资讯
        /// </summary>
        BearingCn = 30,

        /// <summary>
        /// 轴承行业垂直网站（其他）
        /// </summary>
        BearingVerticalSite = 31,

        // ============ 供应商渠道 (40-49) ============

        /// <summary>
        /// 供应商官网
        /// </summary>
        SupplierWebsite = 40,

        // ============ 其他 (90-99) ============

        /// <summary>
        /// 其他
        /// </summary>
        Other = 99
    }
}
