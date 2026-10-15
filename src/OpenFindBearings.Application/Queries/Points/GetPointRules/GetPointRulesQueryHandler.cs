using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Points.GetPointRules
{
    /// <summary>
    /// 轴承币与商家金规则数据源查询处理器（v2.17.0）。
    /// 聚合积分规则表（PointGrantRules）/ 段位表（PointLevels）/ 集体任务定义（MerchantTaskDefinitions）/
    /// SystemConfig / MerchantBuffs 常量，一次下发规则页所需全部数值；改动说明：规则页原硬编码各数值
    /// 与后端多处重复、改配置即漂移，改由本接口单一来源下发，任一来源缺失一律回退 DTO 默认值，
    /// 保证规则页在接口失败时仍可渲染。
    /// </summary>
    public class GetPointRulesQueryHandler : IRequestHandler<GetPointRulesQuery, PointRulesDto>
    {
        private readonly ISystemConfigRepository _configRepository;
        private readonly IPointGrantRuleRepository _ruleRepository;
        private readonly IPointLevelRepository _levelRepository;
        private readonly IMerchantTaskRepository _taskRepository;
        private readonly ILogger<GetPointRulesQueryHandler> _logger;

        /// <summary>构造处理器，注入规则/段位/任务/配置四类仓储</summary>
        public GetPointRulesQueryHandler(
            ISystemConfigRepository configRepository,
            IPointGrantRuleRepository ruleRepository,
            IPointLevelRepository levelRepository,
            IMerchantTaskRepository taskRepository,
            ILogger<GetPointRulesQueryHandler> logger)
        {
            _configRepository = configRepository;
            _ruleRepository = ruleRepository;
            _levelRepository = levelRepository;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        /// <summary>
        /// 处理规则数据源查询：读四类来源并按 grantType/配置键/档位/任务定义装配 DTO，
        /// 每项缺失回退默认（DTO 字段初始值即默认）。
        /// </summary>
        /// <param name="request">规则数据源查询</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>规则数据源 DTO</returns>
        public async Task<PointRulesDto> Handle(GetPointRulesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取轴承币与商家金规则数据源");

            var dto = new PointRulesDto();

            // SystemConfig 整表读入（键值对内存查找，规模有界）
            var configs = await _configRepository.GetAllAsync(cancellationToken);

            /// <summary>按配置键读非负整数，缺失/非法/负数回退默认（与 MerchantGradeService 同口径）</summary>
            int ReadCfg(string key, int fallback)
            {
                var config = configs.FirstOrDefault(c => c.Key == key);
                return config != null && int.TryParse(config.Value, out var v) && v >= 0 ? v : fallback;
            }

            // 积分规则整表读入，按 grantType 匹配
            var rules = await _ruleRepository.GetAllAsync(cancellationToken);

            /// <summary>按动作类型取规则（不存在返回 null）</summary>
            PointGrantRule? Rule(string grantType) =>
                rules.FirstOrDefault(r => string.Equals(r.GrantType, grantType, StringComparison.OrdinalIgnoreCase));

            /// <summary>取规则分值，规则缺失回退默认</summary>
            int Amount(string grantType, int fallback) => Rule(grantType)?.Amount ?? fallback;

            /// <summary>取规则每日上限，规则缺失回退默认</summary>
            int Limit(string grantType, int fallback) => Rule(grantType)?.DailyLimit ?? fallback;

            // ============ 轴承币 ============

            // 签到：阶梯 + 暴击概率同表（daily_checkin 行）
            var checkin = Rule(PointTransaction.TypeDailyCheckin);
            if (checkin != null)
            {
                var ladder = ParseLadder(checkin.LadderJson);
                if (ladder is { Count: > 0 })
                    dto.CheckinLadder = ladder.ToArray();
                dto.CheckinDoubleChance = checkin.DoubleChance;
                dto.CheckinLegendChance = checkin.LegendChance;
            }

            dto.LoginAmount = Amount(PointTransaction.TypeDailyLogin, dto.LoginAmount);
            dto.CorrectionAmount = Amount(PointTransaction.TypeCorrectionAdopted, dto.CorrectionAmount);
            dto.CorrectionDailyLimit = Limit(PointTransaction.TypeCorrectionAdopted, dto.CorrectionDailyLimit);
            dto.ComboAmount = Amount(PointTransaction.TypeDailyCombo, dto.ComboAmount);
            dto.MinigameAmount = Amount(PointTransaction.TypeMinigame, dto.MinigameAmount);
            dto.MinigameDailyLimit = Limit(PointTransaction.TypeMinigame, dto.MinigameDailyLimit);
            dto.MilestoneProfile = Amount(PointTransaction.TypeMerchantProfileComplete, dto.MilestoneProfile);
            dto.MilestoneFirstProduct = Amount(PointTransaction.TypeMerchantFirstProduct, dto.MilestoneFirstProduct);
            dto.MilestoneApproved = Amount(PointTransaction.TypeMerchantApproved, dto.MilestoneApproved);

            // 寻货加量：发布/应答共用同一分值（种子同为 20），日上限各自成表
            dto.SourcingBonusAmount = Amount("sourcing_publish_bonus", dto.SourcingBonusAmount);
            dto.SourcingPublishDailyLimit = Limit("sourcing_publish_bonus", dto.SourcingPublishDailyLimit);
            dto.SourcingRespondDailyLimit = Limit("sourcing_respond_bonus", dto.SourcingRespondDailyLimit);

            // 寻货免费额度走 SystemConfig（Sourcing.*）
            dto.FreePublishPerDay = ReadCfg("Sourcing.FreePublishPerDay", dto.FreePublishPerDay);
            dto.FreeRespondPerDay = ReadCfg("Sourcing.FreeRespondPerDay", dto.FreeRespondPerDay);

            // 段位升档礼区间：启用档位中正升档礼的最小/最大（Lv1 恒 0 不参与）
            var levels = await _levelRepository.GetEnabledAsync(cancellationToken);
            var bonuses = levels.Where(l => l.LevelUpBonus > 0).Select(l => l.LevelUpBonus).ToList();
            if (bonuses.Count > 0)
            {
                dto.LevelBonusMin = bonuses.Min();
                dto.LevelBonusMax = bonuses.Max();
            }

            // 切日时区偏移随 Business.TimeZoneOffsetHours 配置联动，规则页展示用
            dto.TzOffsetHours = ReadCfg("Business.TimeZoneOffsetHours", dto.TzOffsetHours);

            // buff 上限走 MerchantBuffs 代码常量（与发放侧同源）
            dto.BuffCheckinMax = MerchantBuffs.CheckinBonusMax;
            dto.BuffLogin = MerchantBuffs.LoginBonusMax;
            dto.BuffCorrectionMax = MerchantBuffs.CorrectionMultiplierMax;

            // ============ 商家金（SystemConfig 驱动） ============

            dto.TricklePercent = ReadCfg("Business.TricklePercent", dto.TricklePercent);
            dto.TrickleDailyCap = ReadCfg("Business.TrickleDailyCapPerMerchant", dto.TrickleDailyCap);
            dto.TrickleMonthlyCap = ReadCfg("Business.TrickleMonthlyCapPerMerchant", dto.TrickleMonthlyCap);
            dto.GiftSettleMonthlyCap = ReadCfg("Business.GiftSettleMonthlyCap", dto.GiftSettleMonthlyCap);

            // 商家升档礼与保级天数走 MerchantGradeSettings 集中键/默认（与 MerchantGradeService 同源）
            dto.MerchantGradeBonusLv2 = ReadCfg(MerchantGradeSettings.BonusLv2Key, MerchantGradeSettings.BonusLv2Default);
            dto.MerchantGradeBonusLv3 = ReadCfg(MerchantGradeSettings.BonusLv3Key, MerchantGradeSettings.BonusLv3Default);
            dto.MerchantGradeBonusLv4 = ReadCfg(MerchantGradeSettings.BonusLv4Key, MerchantGradeSettings.BonusLv4Default);
            dto.MerchantGradeGraceDays = ReadCfg(MerchantGradeSettings.GraceDaysKey, MerchantGradeSettings.GraceDaysDefault);

            dto.MerchantGoldPayRate = ReadCfg("Business.MerchantGoldPayRate", dto.MerchantGoldPayRate);

            // 集体任务奖励：启用定义中取"周奖励金库"与"成员奖区间"
            var taskDefs = await _taskRepository.GetAllAsync(cancellationToken);
            var enabledTasks = taskDefs.Where(d => d.Enabled).ToList();

            var treasuryWeekly = enabledTasks.FirstOrDefault(d =>
                d.RewardType == MerchantTaskDefinition.RewardTreasury && d.Period == MerchantTaskDefinition.PeriodWeekly);
            if (treasuryWeekly != null)
                dto.MerchantTaskWeeklyTreasury = treasuryWeekly.RewardAmount;

            var memberRewards = enabledTasks
                .Where(d => d.RewardType == MerchantTaskDefinition.RewardMembers)
                .Select(d => d.RewardAmount)
                .ToList();
            if (memberRewards.Count > 0)
            {
                dto.MerchantTaskMemberMin = memberRewards.Min();
                dto.MerchantTaskMemberMax = memberRewards.Max();
            }

            return dto;
        }

        /// <summary>
        /// 解析阶梯 JSON 数组（空/非法返回 null，调用方沿用默认阶梯）
        /// </summary>
        /// <param name="ladderJson">阶梯 JSON 字符串</param>
        /// <returns>阶梯整数列表或 null</returns>
        private static List<int>? ParseLadder(string? ladderJson)
        {
            if (string.IsNullOrWhiteSpace(ladderJson))
                return null;
            try
            {
                return JsonSerializer.Deserialize<List<int>>(ladderJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}