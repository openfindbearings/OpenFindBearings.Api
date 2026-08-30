using FluentValidation;
using System.Text.RegularExpressions;

namespace OpenFindBearings.Application.Commands.SystemConfig.UpdateSystemConfig
{
    /// <summary>
    /// 更新系统配置命令校验器
    /// 职责：在配置写入数据库前校验取值合法性，避免非法值进入库后在运行时被静默回退为默认值
    /// 说明：此前缺少本校验器，管理员把布尔配置填成 "0" 会被 Convert.ChangeType 拒绝并回退为 true，
    ///       与管理意图完全相反且无任何提示
    /// </summary>
    public class UpdateSystemConfigCommandValidator : AbstractValidator<UpdateSystemConfigCommand>
    {
        /// <summary>
        /// 配置值最大长度，与 SystemConfigConfiguration 中 Value 列的长度上限保持一致
        /// </summary>
        private const int MaxValueLength = 1000;

        /// <summary>
        /// 每分钟请求数的允许范围
        /// </summary>
        private const int MinRequestsPerMinute = 1;
        private const int MaxRequestsPerMinute = 100000;

        /// <summary>
        /// 可信度分值的允许范围
        /// </summary>
        private const int MinReliabilityScore = 0;
        private const int MaxReliabilityScore = 100;

        public UpdateSystemConfigCommandValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("配置键不能为空")
                .MaximumLength(100).WithMessage("配置键长度不能超过100个字符");

            // 值允许为空字符串（如备案号、客服联系方式等可留空的展示类配置），仅限制长度
            RuleFor(x => x.Value)
                .MaximumLength(MaxValueLength).WithMessage($"配置值长度不能超过{MaxValueLength}个字符");

            // 价格可见性：限定枚举取值
            When(x => x.Key == "Price.DefaultVisibility" && !string.IsNullOrWhiteSpace(x.Value), () =>
            {
                RuleFor(x => x.Value)
                    .Must(v => v is "Public" or "LoginRequired")
                    .WithMessage("价格默认可见性只能为 Public 或 LoginRequired");
            });

            // 布尔型配置：限定宽松布尔写法，与 SystemConfigRepository.TryParseBool 的识别范围一致
            When(x => x.Key is "Price.ShowNegotiableLabel" or "Price.NumericForSorting", () =>
            {
                RuleFor(x => x.Value)
                    .Must(IsRecognizedBoolean)
                    .WithMessage("布尔型配置只能填 true/false、1/0、yes/no、on/off");
            });

            // 价格提取正则：必须是可编译的正则表达式，否则商品价格会被静默解析为空
            When(x => x.Key == "Price.ExtractPattern" && !string.IsNullOrWhiteSpace(x.Value), () =>
            {
                RuleFor(x => x.Value)
                    .Must(IsCompilableRegex)
                    .WithMessage("价格提取正则不是合法的正则表达式");
            });

            // 限流阈值：必须为正整数且在合理范围内
            When(x => x.Key.StartsWith("RateLimit.") && x.Key.EndsWith(".RequestsPerMinute"), () =>
            {
                RuleFor(x => x.Value)
                    .Must(v => int.TryParse(v, out var n) && n >= MinRequestsPerMinute && n <= MaxRequestsPerMinute)
                    .WithMessage($"每分钟请求数必须为 {MinRequestsPerMinute} 到 {MaxRequestsPerMinute} 之间的整数");
            });

            // 可信度阈值与来源基础分：必须为 0 到 100 的整数
            When(x => x.Key is "Reliability.AutoSyncThreshold"
                           or "Reliability.ReviewThreshold"
                           or "Reliability.DefaultSourceScore", () =>
            {
                RuleFor(x => x.Value)
                    .Must(v => int.TryParse(v, out var n) && n >= MinReliabilityScore && n <= MaxReliabilityScore)
                    .WithMessage($"可信度分值必须为 {MinReliabilityScore} 到 {MaxReliabilityScore} 之间的整数");
            });
        }

        /// <summary>
        /// 判断是否为可识别的布尔写法
        /// </summary>
        /// <param name="value">配置值文本</param>
        /// <returns>是否属于 true/false、1/0、yes/no、y/n、on/off 之一</returns>
        private static bool IsRecognizedBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Trim().ToLowerInvariant() switch
            {
                "true" or "1" or "yes" or "y" or "on" => true,
                "false" or "0" or "no" or "n" or "off" => true,
                _ => false
            };
        }

        /// <summary>
        /// 判断配置值是否为可编译的正则表达式
        /// </summary>
        /// <param name="value">配置值文本</param>
        /// <returns>语法合法返回 true</returns>
        private static bool IsCompilableRegex(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                _ = new Regex(value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
