using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    /// <summary>
    /// 参考数据种子：积分发放规则、段位档位、勋章定义、商城道具与商家集体任务。
    /// 改动说明：这些数据原先散落在多个迁移里（AddPointSystem、AddAchievements、AddMall、
    ///   AddMerchantTasks、AddLevelGameplay 等），迁移收缩后集中到本文件的幂等 SQL 脚本。
    ///   脚本逐条判重（WHERE NOT EXISTS / ON CONFLICT），可重复执行；不覆盖后台已调整的数值。
    /// </summary>
    public static partial class SeedData
    {
        /// <summary>
        /// 参考数据脚本。来源为历史迁移中的数据语句，按原顺序合并：
        /// 先插入默认行，再执行术语与启用状态的收敛更新，最终状态与存量库一致。
        /// </summary>
        private const string ReferenceDataSql = """
-- 勋章解锁规则：成就已纯荣誉化，保留规则行但停用（与存量库最终状态一致）
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'achievement_unlock', '勋章解锁奖励', 5, 0, NULL, false, '勋章点亮一次性可花积分甜头（实际分值按勋章定义覆盖）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'achievement_unlock');

-- ===== rules.基础五条 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'daily_login', '每日登录', 1, 0, NULL, true, '当日首次业务请求自动发放（被动保底）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'daily_login');
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'daily_checkin', '每日签到', 2, 0, '[2,3,4,5,5]', true, '主动签到，连续天数阶梯（1/2/3/4/5+天），超末档取末档', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'daily_checkin');
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'register_bonus', '新用户注册奖励', 50, 0, NULL, true, '首次创建业务用户一次性发放（冷启动钩子）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'register_bonus');
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'correction_adopted', '纠错被采纳', 20, 100, NULL, true, '纠错审核采纳发放，每日上限 100 防灌水', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'correction_adopted');
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'merchant_approved', '商户入驻通过', 100, 0, NULL, true, '入驻审核通过一次性发放（供给侧最高价值动作）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'merchant_approved');

-- ===== rules.户与商品首单 =====

                
                INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), v.g, v.n, v.a, 0, NULL, true, v.d, now(), true
                FROM (VALUES
                    ('merchant_profile_complete', '完善商户资料', 50, '联系人/电话/简介/地址首次齐全（同执照仅一次）'),
                    ('merchant_first_product', '首件商品上架', 20, '商户首个在售商品上架（同执照仅一次）')
                ) AS v(g, n, a, d)
                WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" r WHERE r."GrantType" = v.g);

                UPDATE "PointGrantRules" SET "IsEnabled" = false, "UpdatedAt" = now()
                WHERE "GrantType" = 'register_bonus' AND "IsEnabled" = true;

-- ===== rules.寻货加量 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), v.g, v.n, v.a, v.l, NULL, true, v.d, now(), true
                FROM (VALUES
                    ('sourcing_publish_bonus', '寻货发布积分加量', 20, 10, '免费额度用完后花积分继续发布（20 分/条，硬上限 10 条/天）'),
                    ('sourcing_respond_bonus', '寻货应答积分加量', 20, 50, '免费额度用完后花积分继续应答（20 分/条，硬上限 50 条/天）')
                ) AS v(g, n, a, l, d)
                WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" r WHERE r."GrantType" = v.g);

                INSERT INTO "SystemConfigs" ("Id", "Key", "Value", "Description", "Group", "ValueType", "IsSystem", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(), v.k, v.val, v.d, 'Sourcing', 'number', false, now(), now(), true
                FROM (VALUES
                    ('Sourcing.FreePublishPerDay', '3', '个人每日免费发布寻货条数（超出后每条扣积分）'),
                    ('Sourcing.FreeRespondPerDay', '20', '商户每日免费应答条数（超出后每条扣积分）')
                ) AS v(k, val, d)
                WHERE NOT EXISTS (SELECT 1 FROM "SystemConfigs" s WHERE s."Key" = v.k);

-- ===== rules.商家任务 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'merchant_task', '商家集体任务奖励', 20, 0, NULL, true, '商家集体任务达标结算发放（分值按任务定义覆盖，Job 自动发放）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" IN ('merchant_task', 'guild_task'));

-- ===== rules.每日三件套 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), 'daily_combo', '每日任务板三件套', 30, 1, NULL, true, '今日 签到 + 纠错被采纳 + 寻货应答 三项全完成后额外 +30 分', now(), true
                WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" r WHERE r."GrantType" = 'daily_combo');

                INSERT INTO "PointLevels" ("Id", "Level", "MinTotalEarned", "Name", "Enabled", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(), v.lv, v.min, v.n, true, now(), now(), true
                FROM (VALUES
                    (1, 0,    '初出茅庐'),
                    (2, 100,  '小有所成'),
                    (3, 300,  '崭露头角'),
                    (4, 600,  '渐入佳境'),
                    (5, 1000, '炉火纯青'),
                    (6, 1600, '行家里手'),
                    (7, 2400, '登堂入室'),
                    (8, 3400, '出类拔萃'),
                    (9, 4600, '登峰造极'),
                    (10, 6000, '独孤求败')
                ) AS v(lv, min, n)
                WHERE NOT EXISTS (SELECT 1 FROM "PointLevels" p WHERE p."Level" = v.lv);

-- ===== rules.小游戏 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'minigame', '小游戏胜利奖励', 5, 10, NULL, true, '轴承连连看等平台小游戏胜利结算发放（每局按 gameId 幂等，日限防刷）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" WHERE "GrantType" = 'minigame');

-- ===== rules.段位升档 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'level_up_bonus', '段位升档礼', 10, 0, NULL, true, '累计获得轴承币跨入新段位时一次性发放（金额按档位表 LevelUpBonus 覆盖，每用户每档终身一次）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" r WHERE r."GrantType" = 'level_up_bonus');

-- ===== rules.签到暴击 =====
UPDATE "PointGrantRules" SET "DoubleChance" = 10, "LegendChance" = 2, "UpdatedAt" = now()
                WHERE "GrantType" = 'daily_checkin';

                INSERT INTO "AchievementDefinitions" ("Id", "Key", "Name", "Description", "Icon", "ImageKey", "Scope", "Category", "MetricKey", "ProgressTarget", "MetaPoints", "TitleReward", "Rare", "Hidden", "IsLimited", "LimitedOrdinal", "Enabled", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(), 'founder_elder', '创站元老', '平台前 100 位注册用户专属徽章，窗口已关闭，绝版不返场', 'medal', NULL, 1, '元老', 'founder_ordinal', 1, 60, '创站元老', true, true, true, 100, true, now(), now(), true
                WHERE NOT EXISTS (SELECT 1 FROM "AchievementDefinitions" WHERE "Key" = 'founder_elder');

-- ===== rules.术语收敛 =====
UPDATE "SystemConfigs" SET "Key" = 'Business.MerchantPremiumOnSaleMin'
 WHERE "Key" = 'Business.GuildPremiumOnSaleMin'
   AND NOT EXISTS (SELECT 1 FROM "SystemConfigs" s WHERE s."Key" = 'Business.MerchantPremiumOnSaleMin');
UPDATE "SystemConfigs" SET "Key" = 'Business.MerchantGoldOnSaleMin'
 WHERE "Key" = 'Business.GuildGoldOnSaleMin'
   AND NOT EXISTS (SELECT 1 FROM "SystemConfigs" s WHERE s."Key" = 'Business.MerchantGoldOnSaleMin');
UPDATE "SystemConfigs" SET "Key" = 'Business.MerchantGoldTreasuryMin'
 WHERE "Key" = 'Business.GuildGoldTreasuryMin'
   AND NOT EXISTS (SELECT 1 FROM "SystemConfigs" s WHERE s."Key" = 'Business.MerchantGoldTreasuryMin');
DELETE FROM "SystemConfigs" WHERE "Key" IN ('Business.GuildPremiumOnSaleMin', 'Business.GuildGoldOnSaleMin', 'Business.GuildGoldTreasuryMin');
UPDATE "PointGrantRules" SET "GrantType" = 'merchant_task', "DisplayName" = '商家集体任务奖励', "Description" = '商家集体任务达标结算发放（分值按任务定义覆盖，Job 自动发放）'
 WHERE "GrantType" = 'guild_task';

-- ===== rules.术语收敛 =====
UPDATE "PointGrantRules" SET "DisplayName" = '勋章解锁奖励', "Description" = '勋章点亮一次性可花积分甜头（实际分值按勋章定义覆盖）'
 WHERE "GrantType" = 'achievement_unlock';

-- ===== levels.十档 =====
INSERT INTO "PointGrantRules" ("Id", "GrantType", "DisplayName", "Amount", "DailyLimit", "LadderJson", "IsEnabled", "Description", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), 'daily_combo', '每日任务板三件套', 30, 1, NULL, true, '今日 签到 + 纠错被采纳 + 寻货应答 三项全完成后额外 +30 分', now(), true
                WHERE NOT EXISTS (SELECT 1 FROM "PointGrantRules" r WHERE r."GrantType" = 'daily_combo');

                INSERT INTO "PointLevels" ("Id", "Level", "MinTotalEarned", "Name", "Enabled", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(), v.lv, v.min, v.n, true, now(), now(), true
                FROM (VALUES
                    (1, 0,    '初出茅庐'),
                    (2, 100,  '小有所成'),
                    (3, 300,  '崭露头角'),
                    (4, 600,  '渐入佳境'),
                    (5, 1000, '炉火纯青'),
                    (6, 1600, '行家里手'),
                    (7, 2400, '登堂入室'),
                    (8, 3400, '出类拔萃'),
                    (9, 4600, '登峰造极'),
                    (10, 6000, '独孤求败')
                ) AS v(lv, min, n)
                WHERE NOT EXISTS (SELECT 1 FROM "PointLevels" p WHERE p."Level" = v.lv);

-- ===== levels.段位名与升档礼 =====
UPDATE "PointLevels" p SET "Name" = v.n FROM (VALUES
 (1, '倔强青铜'),
 (2, '坚韧青铜'),
 (3, '秩序白银'),
 (4, '璀璨白银'),
 (5, '荣耀黄金'),
 (6, '华彩铂金'),
 (7, '永恒钻石'),
 (8, '至尊星耀'),
 (9, '傲世星耀'),
 (10, '最强王者')
) AS v(lv, n)
WHERE p."Level" = v.lv;

-- ===== levels.段位名与升档礼 =====
UPDATE "PointLevels" p SET "LevelUpBonus" = v.b FROM (VALUES
 (1, 0),
 (2, 10),
 (3, 15),
 (4, 20),
 (5, 30),
 (6, 40),
 (7, 55),
 (8, 70),
 (9, 90),
 (10, 120)
) AS v(lv, b)
WHERE p."Level" = v.lv;

-- ===== achievements.数据共创与成就 =====
INSERT INTO "AchievementDefinitions" ("Id", "Key", "Name", "Description", "Icon", "Scope", "Category", "MetricKey", "ProgressTarget", "MetaPoints", "TitleReward", "Rare", "Hidden", "Enabled", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Key", v."Name", v."Description", v."Icon", v."Scope", v."Category", v."MetricKey", v."Target", v."Meta", v."Title", v."Rare", v."Hidden", true, now(), true
FROM (VALUES
    ('correction_first','初次纠错','首条纠错被平台采纳','edit',1,'数据共创','correction_adopted',1,10,NULL,false,false),
    ('correction_10','火眼金睛','累计 10 条纠错被采纳','search',1,'数据共创','correction_adopted',10,30,NULL,false,false),
    ('correction_50','数据纠错师','累计 50 条纠错被采纳','award',1,'数据共创','correction_adopted',50,80,'数据纠错师',false,false),
    ('correction_100','数据守护神','累计 100 条纠错被采纳','shield',1,'数据共创','correction_adopted',100,150,'数据守护神',true,false),
    ('checkin_7','七日之约','连续签到 7 天','calendar',1,'忠诚','checkin_streak',7,15,NULL,false,false),
    ('checkin_30','月度坚守','连续签到 30 天','calendar-check',1,'忠诚','checkin_streak',30,40,'坚守者',false,false),
    ('checkin_100','百日之约','连续签到 100 天','star',1,'忠诚','checkin_streak',100,100,NULL,true,false),
    ('checkin_total_30','签到达人','累计签到 30 次','check',1,'忠诚','checkin_total',30,20,NULL,false,false),
    ('sourcing_selected_first','伯乐','应答首次被发布人选定','handshake',1,'寻货','sourcing_selected',1,20,NULL,false,false),
    ('sourcing_selected_10','金牌红娘','累计 10 次应答被选定','medal',1,'寻货','sourcing_selected',10,60,'金牌红娘',false,false),
    ('merchant_first_listing','开张大吉','首件商品上架在售','store',2,'商户','listing_count',1,20,NULL,false,false),
    ('merchant_listing_20','货架满满','在售商品达 20 件','boxes',2,'商户','listing_count',20,50,NULL,false,false),
    ('merchant_verified','认证商家','通过平台资质认证','badge-check',2,'商户','merchant_verified',1,40,'认证商家',false,false),
    ('merchant_selected_first','首单中选','应答首次被发布人选定','handshake',2,'商户','merchant_selected',1,20,NULL,false,false),
    ('merchant_selected_10','金牌供应商','累计 10 次应答被选定','trophy',2,'商户','merchant_selected',10,60,NULL,true,false)
) AS v("Key", "Name", "Description", "Icon", "Scope", "Category", "MetricKey", "Target", "Meta", "Title", "Rare", "Hidden")
WHERE NOT EXISTS (SELECT 1 FROM "AchievementDefinitions" a WHERE a."Key" = v."Key");

-- ===== achievements.新手旅程 =====
INSERT INTO "AchievementDefinitions" ("Id", "Key", "Name", "Description", "Icon", "Scope", "Category", "MetricKey", "ProgressTarget", "MetaPoints", "TitleReward", "Rare", "Hidden", "Enabled", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Key", v."Name", v."Description", v."Icon", v."Scope", v."Category", v."MetricKey", v."Target", v."Meta", v."Title", v."Rare", v."Hidden", true, now(), true
FROM (VALUES
    ('checkin_first','初次签到','完成首次每日签到','check',1,'新手旅程','checkin_total',1,5,NULL,false,false),
    ('checkin_3','三日之约','连续签到 3 天','calendar',1,'新手旅程','checkin_streak',3,10,NULL,false,false),
    ('checkin_total_10','签到集锦','累计签到 10 次','calendar-check',1,'新手旅程','checkin_total',10,25,NULL,false,false),
    ('register_first','初来乍到','注册加入平台','sparkles',1,'新手旅程','register_total',1,5,NULL,false,false),
    ('login_7','七日陪伴','累计登录 7 天','flame',1,'新手旅程','login_total',7,20,NULL,false,false),
    ('sourcing_first','旗开得胜','发布首条寻货需求','flag',1,'新手旅程','sourcing_publish_total',1,15,NULL,false,false)
) AS v("Key", "Name", "Description", "Icon", "Scope", "Category", "MetricKey", "Target", "Meta", "Title", "Rare", "Hidden")
WHERE NOT EXISTS (SELECT 1 FROM "AchievementDefinitions" a WHERE a."Key" = v."Key");

-- ===== achievements.创站元老 =====
UPDATE "PointGrantRules" SET "DoubleChance" = 10, "LegendChance" = 2, "UpdatedAt" = now()
                WHERE "GrantType" = 'daily_checkin';

                INSERT INTO "AchievementDefinitions" ("Id", "Key", "Name", "Description", "Icon", "ImageKey", "Scope", "Category", "MetricKey", "ProgressTarget", "MetaPoints", "TitleReward", "Rare", "Hidden", "IsLimited", "LimitedOrdinal", "Enabled", "CreatedAt", "UpdatedAt", "IsActive")
SELECT gen_random_uuid(), 'founder_elder', '创站元老', '平台前 100 位注册用户专属徽章，窗口已关闭，绝版不返场', 'medal', NULL, 1, '元老', 'founder_ordinal', 1, 60, '创站元老', true, true, true, 100, true, now(), now(), true
                WHERE NOT EXISTS (SELECT 1 FROM "AchievementDefinitions" WHERE "Key" = 'founder_elder');

-- ===== mall.商品置顶卡 =====
INSERT INTO "MallItems" ("Id", "Key", "Name", "Description", "Icon", "Category", "PointPrice", "FlashPrice", "FlashStart", "FlashEnd", "DurationHours", "Stock", "SoldCount", "Enabled", "SortOrder", "AuditState", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Key", v."Name", v."Description", v."Icon", v."Category", v."Price", NULL, NULL, NULL, v."Hours", v."Stock", 0, true, v."Sort", 0, now(), true
FROM (VALUES
 ('pin_24h', '24小时置顶卡', '把一件在售商品在该型号商家列表置顶 24 小时，买家一眼先看到你', 'arrow-up-circle', 1, 120, 24, -1, 10),
 ('pin_72h', '72小时置顶卡', '把一件在售商品在该型号商家列表置顶 72 小时，长曝光更划算', 'rocket', 1, 320, 72, -1, 20)
) AS v("Key", "Name", "Description", "Icon", "Category", "Price", "Hours", "Stock", "Sort")
WHERE NOT EXISTS (SELECT 1 FROM "MallItems" m WHERE m."Key" = v."Key");

-- ===== mall.寻货置顶卡与改名 =====
INSERT INTO "MallItems" ("Id", "Key", "Name", "Description", "Icon", "Category", "PointPrice", "DurationHours", "Stock", "SoldCount", "Enabled", "SortOrder", "TargetKind", "AuditState", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), v."Key", v."Name", v."Description", v."Icon", 1, v."PointPrice", v."DurationHours", -1, 0, true, v."SortOrder", 2, 0, (now() at time zone 'utc'), true
FROM (VALUES
    ('demand_pin_24h', '24小时寻货置顶卡', '把你发布的一条寻货需求在公开大厅置顶 24 小时，商家优先看到（个人积分支付）', 'pin', 60, 24, 30),
    ('demand_pin_72h', '72小时寻货置顶卡', '把你发布的一条寻货需求在公开大厅置顶 72 小时，曝光拉满（个人积分支付）', 'rocket', 160, 72, 40)
) AS v("Key", "Name", "Description", "Icon", "PointPrice", "DurationHours", "SortOrder")
WHERE NOT EXISTS (SELECT 1 FROM "MallItems" m WHERE m."Key" = v."Key");

-- ===== mall.寻货置顶卡与改名 =====
UPDATE "MallItems" SET "Name" = '24小时商品置顶卡',
    "Description" = '让一个在售商品在该型号商家列表置顶 24 小时（商家金定价，管理员金库支付；个人代付按汇率折算）',
    "TargetKind" = 1, "UpdatedAt" = (now() at time zone 'utc')
WHERE "Key" = 'pin_24h' AND "Name" = '24小时置顶卡';
UPDATE "MallItems" SET "Name" = '72小时商品置顶卡',
    "Description" = '让一个在售商品在该型号商家列表置顶 72 小时（商家金定价，管理员金库支付；个人代付按汇率折算）',
    "TargetKind" = 1, "UpdatedAt" = (now() at time zone 'utc')
WHERE "Key" = 'pin_72h' AND "Name" = '72小时置顶卡';

-- ===== merchantTasks.三条集体任务 =====
INSERT INTO "MerchantTaskDefinitions" ("Id", "TaskKey", "Name", "Description", "MetricKey", "TargetValue", "Period", "RewardType", "RewardAmount", "Enabled", "SortOrder", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'merchant_corrections_week', '全员纠错周', '本自然周内成员纠错被采纳合计达标，全体在职成员各得积分', 'corrections', 5, 1, 1, 20, true, 1, now(), true
WHERE NOT EXISTS (SELECT 1 FROM "MerchantTaskDefinitions" WHERE "TaskKey" = 'merchant_corrections_week');
INSERT INTO "MerchantTaskDefinitions" ("Id", "TaskKey", "Name", "Description", "MetricKey", "TargetValue", "Period", "RewardType", "RewardAmount", "Enabled", "SortOrder", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'merchant_products_week', '全员上新周', '本自然周内新上架商品合计达标，商家金库获得推广奖励', 'products', 5, 1, 2, 100, true, 2, now(), true
WHERE NOT EXISTS (SELECT 1 FROM "MerchantTaskDefinitions" WHERE "TaskKey" = 'merchant_products_week');
INSERT INTO "MerchantTaskDefinitions" ("Id", "TaskKey", "Name", "Description", "MetricKey", "TargetValue", "Period", "RewardType", "RewardAmount", "Enabled", "SortOrder", "CreatedAt", "IsActive")
SELECT gen_random_uuid(), 'merchant_treasury_month', '金库冲刺月', '本自然月金库入账合计达标，全体在职成员各得积分', 'treasury', 300, 2, 1, 50, true, 3, now(), true
WHERE NOT EXISTS (SELECT 1 FROM "MerchantTaskDefinitions" WHERE "TaskKey" = 'merchant_treasury_month');
""";

        /// <summary>
        /// 幂等写入参考数据。任何一条语句失败都会抛出并由上层日志记录，便于快速暴露而不是静默留空。
        /// </summary>
        private static async Task EnsureReferenceDataAsync(ApplicationDbContext context, ILogger logger)
        {
            await context.Database.ExecuteSqlRawAsync(ReferenceDataSql);
            logger.LogInformation("参考数据（积分规则/段位/勋章/商城道具/商家任务）已校验完成");
        }
    }
}
