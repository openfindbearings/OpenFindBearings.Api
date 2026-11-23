using Microsoft.EntityFrameworkCore;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    /// <summary>
    /// 系统配置键种子：集中维护全部配置键的默认值与元信息。
    /// 改动说明：原先配置键分散在种子主流程与多个迁移里（迁移各插一段 SystemConfigs），
    ///   收缩迁移后统一收敛到本文件，作为配置键的唯一权威来源。
    /// </summary>
    public static partial class SeedData
    {
        /// <summary>
        /// 系统配置默认值定义（键、默认值、分组、说明、值类型、是否系统项）。
        /// 与迁移收缩前的最终库状态保持一致：描述文案取重命名后的最终措辞（口碑商家）。
        /// </summary>
        private static readonly (string Key, string Value, string Group, string Description, string ValueType, bool IsSystem)[] ConfigDefaults =
        [
            // 站点设置，供移动端 /api/mobile/config 与 Admin 页脚消费
            ("SiteName", "OpenFindBearings", "Site", "网站名称", "string", true),
            ("SiteDescription", "轴承信息平台", "Site", "网站描述", "string", true),
            ("Site.PlatformName", "OpenFindBearings", "Site", "平台名称（海报落款等对外展示）", "string", true),
            ("Site.BeiAn", "", "Site", "网站备案号（Admin 页脚与 H5 端关于页）", "string", true),
            ("Site.CustomerService", "", "Site", "客服电话（全端共用：app 设置页拨打与法务文本）", "string", true),

            // 价格显示
            ("Price.DefaultVisibility", "LoginRequired", "Price", "价格默认可见性", "string", true),
            ("Price.ShowNegotiableLabel", "true", "Price", "是否显示议价标签", "bool", true),
            ("Price.NumericForSorting", "true", "Price", "是否启用数值化价格", "bool", true),
            ("Price.ExtractPattern", @"¥(\d+(?:\.\d+)?)", "Price", "价格提取正则（需含首个捕获组）", "string", false),

            // 限流与可信度阈值
            ("RateLimit.Guest.RequestsPerMinute", "30", "System", "游客每分钟请求数", "int", true),
            ("RateLimit.User.RequestsPerMinute", "60", "System", "用户每分钟请求数", "int", true),
            ("RateLimit.Premium.RequestsPerMinute", "120", "System", "付费用户每分钟请求数", "int", true),
            ("Reliability.AutoSyncThreshold", "85", "System", "自动同步阈值（可信度达到此值直接入库）", "int", false),
            ("Reliability.ReviewThreshold", "60", "System", "人工审核阈值（可信度达到此值进入待审核）", "int", false),
            ("Reliability.DefaultSourceScore", "80", "System", "来源默认基础分", "int", false),

            // 移动端版本更新，供 /api/mobile/version/check 消费
            ("Mobile.AppVersion", "v1.0.0-rc.1", "Mobile", "移动端最新版本号（SemVer 带点 prerelease）", "string", true),
            ("Mobile.MinVersion", "v1.0.0-rc.1", "Mobile", "最低支持版本（低于此值且开启强更时强制更新）", "string", true),
            ("Mobile.ForceUpdate", "false", "Mobile", "强制更新开关", "bool", true),
            ("Mobile.DownloadUrl", "https://bff.515813.xyz/dl/", "Mobile", "APK 下载目录（以/结尾，客户端按 app-v版本-ABI.apk 拼文件名）", "string", true),
            ("Mobile.UpdateMessage", "发现新版本，建议更新", "Mobile", "更新弹窗说明文案", "string", true),
            ("Mobile.MediaBaseUrl", "https://bff.515813.xyz/media", "Mobile", "媒体资源公网 base（末尾无斜杠）", "string", true),
            ("Mobile.BeiAnApp", "", "Mobile", "App 备案号（RN 端关于页展示）", "string", true),
            ("Mobile.BeiAnMini", "", "Mobile", "小程序备案号（微信端关于页展示）", "string", true),

            // 寻货配额
            ("Sourcing.FreePublishPerDay", "3", "Sourcing", "个人每日免费发布寻货条数（超出后每条扣积分）", "int", false),
            ("Sourcing.FreeRespondPerDay", "20", "Sourcing", "商户每日免费应答条数（超出后每条扣积分）", "int", false),
            ("Sourcing.NotifyMaxPerMerchantPerDay", "3", "Sourcing", "同一商户管理员同型号每日寻货提醒上限", "int", false),

            // 业务日界
            ("Business.TimeZoneOffsetHours", "8", "Business", "业务时区偏移小时数：日任务/签到/额度的切日边界（默认 8=北京时间），修改后需重启 API 生效", "int", false),

            // 商户金库与挂礼
            ("Business.TricklePercent", "10", "Business", "成员合格赚分上供商户金库的比例（百分比，0=关闭）", "int", true),
            ("Business.TrickleDailyCapPerMerchant", "50", "Business", "单商户金库每日 trickle 入账上限", "int", true),
            ("Business.TrickleMonthlyCapPerMerchant", "1000", "Business", "单商户金库每月 trickle 入账上限", "int", true),
            ("Business.GiftSettleMonthlyCap", "2000", "Business", "单商户金库每月挂礼结算上限", "int", true),
            ("Business.GiftReceiverMonthlyLimit", "3", "Business", "同一收货电话/地址每月礼品兑换单数上限（防刷闸）", "int", true),
            ("Business.GiftPriceCeiling", "3000", "Business", "商家礼品平台定档单价封顶（防定向积分转移）", "int", true),

            // 商户卡级与升档礼
            ("Business.MerchantPremiumOnSaleMin", "5", "Business", "商家 Lv3 口碑商家：认证+在售达此数", "int", true),
            ("Business.MerchantGoldOnSaleMin", "10", "Business", "商家 Lv4 金牌：认证+在售达此数", "int", true),
            ("Business.MerchantGoldTreasuryMin", "500", "Business", "商家 Lv4 金牌：金库累计入账达此数", "int", true),
            ("Business.MerchantGoldPayRate", "2", "Business", "商品置顶个人代付汇率：1 商家金折算 N 个人积分（仅商家管理员个人通道生效）", "int", true),
            ("Business.MerchantGradeGraceDays", "15", "Business", "商家等级降档保级缓冲天数（重算不达标后维持原档的宽限期，0=立即降档）", "int", true),
            ("Business.MerchantGradeBonusLv2", "50", "Business", "商家升到 Lv2 认证的一次性升档礼（每商户每档终身一次）", "int", true),
            ("Business.MerchantGradeBonusLv3", "150", "Business", "商家升到 Lv3 口碑商家的一次性升档礼（每商户每档终身一次）", "int", true),
            ("Business.MerchantGradeBonusLv4", "300", "Business", "商家升到 Lv4 金牌的一次性升档礼（每商户每档终身一次）", "int", true),
            ("Business.MerchantSilverDealMin", "1", "Business", "银卡：近窗口期成功撮合次数下限", "int", true),
            ("Business.MerchantGoldDealMin", "3", "Business", "金卡：近窗口期成功撮合次数下限", "int", true),
            ("Business.MerchantGradeWindowDays", "180", "Business", "卡级成功撮合统计滚动窗口天数", "int", true),

            // 成交回执反刷
            ("Business.SourcingFeedbackMinAccountAgeDays", "1", "Business", "成交回执计分门槛：需求方账号最短注册天数（0=不校验）", "int", true),
            ("Business.SourcingFeedbackIpWindowHours", "24", "Business", "成交回执反刷：同 IP 异常检测时间窗（小时）", "int", true),
            ("Business.SourcingFeedbackIpMax", "5", "Business", "成交回执反刷：同 IP 时间窗内允许的计分回执上限", "int", true),

            // 意见反馈
            ("Business.FeedbackDailyLimit", "5", "Business", "意见反馈每日提交上限（按业务日界计算，0=不限制）", "int", true),
        ];

        /// <summary>
        /// 幂等补全系统配置键：只补缺失键，不覆盖已有值（运维在 Admin 调过的值必须保留）。
        /// </summary>
        private static async Task EnsureConfigKeysAsync(ApplicationDbContext context)
        {
            var existingKeys = await context.SystemConfigs
                .Select(c => c.Key)
                .ToListAsync();

            var existingSet = existingKeys.ToHashSet(StringComparer.Ordinal);

            var missing = ConfigDefaults
                .Where(d => !existingSet.Contains(d.Key))
                .Select(d => new Domain.Entities.SystemConfig(d.Key, d.Value, d.Group, d.Description, d.ValueType, d.IsSystem))
                .ToList();

            if (missing.Count == 0)
            {
                return;
            }

            await context.SystemConfigs.AddRangeAsync(missing);
            await context.SaveChangesAsync();
        }
    }
}