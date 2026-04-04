namespace OpenFindBearings.Domain.Enums.Extensions
{
    /// <summary>
    /// CrawlerSourceSite 扩展方法
    /// </summary>
    public static class CrawlerSourceSiteExtensions
    {
        /// <summary>
        /// 获取可信度分数（0-100）
        /// </summary>
        public static int GetReliabilityScore(this CrawlerSourceSite site)
        {
            return site switch
            {
                // 国际品牌官网 - 最高可信度 95
                CrawlerSourceSite.SkfOfficial or
                CrawlerSourceSite.FagOfficial or
                CrawlerSourceSite.NskOfficial or
                CrawlerSourceSite.NtnOfficial or
                CrawlerSourceSite.TimkenOfficial => 95,

                // 行业协会/权威机构 - 高可信度 90
                // 理由：官方机构，数据权威，但可能不是原始产品数据源
                CrawlerSourceSite.ChinaBearingIndustryAssociation => 90,

                // 供应商官网 - 较高可信度 85
                // 理由：一手数据，但可能有夸大宣传
                CrawlerSourceSite.SupplierWebsite => 85,

                // 行业垂直网站 - 中等可信度 75
                // 佰联轴承网：数据较全，但可能来自多方聚合，需交叉验证
                CrawlerSourceSite.BearingCn => 75,
                CrawlerSourceSite.BearingVerticalSite => 75,

                // 电商平台 - 较低可信度 60-65
                // 理由：商家自行发布，可能存在错误或夸大
                CrawlerSourceSite.Alibaba or
                CrawlerSourceSite.Taobao => 60,  // C2C平台，可信度更低
                CrawlerSourceSite.JD => 65,       // B2C平台，相对好一些

                // 其他
                CrawlerSourceSite.Other => 70,
                CrawlerSourceSite.Unknown => 50,

                _ => 70
            };
        }

        /// <summary>
        /// 获取网站显示名称
        /// </summary>
        public static string GetDisplayName(this CrawlerSourceSite site)
        {
            return site switch
            {
                // 国际品牌官网
                CrawlerSourceSite.SkfOfficial => "SKF官方网站",
                CrawlerSourceSite.FagOfficial => "FAG/INA官方网站",
                CrawlerSourceSite.NskOfficial => "NSK官方网站",
                CrawlerSourceSite.NtnOfficial => "NTN官方网站",
                CrawlerSourceSite.TimkenOfficial => "TIMKEN官方网站",

                // 行业协会
                CrawlerSourceSite.ChinaBearingIndustryAssociation => "中国轴承工业协会 (www.cbia.com.cn)",

                // 电商平台
                CrawlerSourceSite.Alibaba => "阿里巴巴(1688.com)",
                CrawlerSourceSite.Taobao => "淘宝网",
                CrawlerSourceSite.JD => "京东商城",

                // 行业垂直网站
                CrawlerSourceSite.BearingCn => "佰联轴承网 (www.bearing.cn)",
                CrawlerSourceSite.BearingVerticalSite => "轴承行业垂直网站",

                // 供应商
                CrawlerSourceSite.SupplierWebsite => "供应商官网",

                // 其他
                CrawlerSourceSite.Other => "其他网站",
                CrawlerSourceSite.Unknown => "未知来源",

                _ => "未知来源"
            };
        }

        /// <summary>
        /// 是否为官方/权威数据源
        /// </summary>
        public static bool IsOfficial(this CrawlerSourceSite site)
        {
            return site is (>= CrawlerSourceSite.SkfOfficial and <= CrawlerSourceSite.TimkenOfficial)
                or CrawlerSourceSite.ChinaBearingIndustryAssociation;
        }
    }
}
