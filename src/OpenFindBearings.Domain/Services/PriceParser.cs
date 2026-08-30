using System.Text.RegularExpressions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Services
{
    /// <summary>
    /// 价格文本解析领域服务
    /// 职责：从多样的价格描述文本中提取数值价格，用于排序与筛选
    /// 说明：实际业务中价格表述多样（"¥55-60"、"约500元"、"电议"、"面议"），
    ///       本服务依据可配置的正则表达式提取首个数值，无法解析时返回 null
    /// </summary>
    public static class PriceParser
    {
        /// <summary>
        /// 从价格描述文本中提取数值价格
        /// </summary>
        /// <param name="description">价格描述文本，如 "¥55-60"</param>
        /// <param name="pattern">提取正则表达式，取自系统配置 Price.ExtractPattern，需包含首个捕获组</param>
        /// <returns>解析成功返回数值价格；描述为空、正则无效或无匹配时返回 null</returns>
        public static decimal? ExtractNumericPrice(string? description, string? pattern)
        {
            if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(pattern))
                return null;

            try
            {
                // 正则超时保护，避免管理员配置了灾难性回溯的表达式导致请求挂死
                var match = Regex.Match(description, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                if (!match.Success || match.Groups.Count < 2)
                    return null;

                if (decimal.TryParse(match.Groups[1].Value, out var value))
                    return value;

                return null;
            }
            catch (ArgumentException)
            {
                // 正则语法非法时降级为不解析，不阻断主流程
                return null;
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
        }

        /// <summary>
        /// 将配置中的价格可见性文本转换为枚举
        /// </summary>
        /// <param name="value">配置值文本，取自系统配置 Price.DefaultVisibility（Public / LoginRequired）</param>
        /// <returns>可见性枚举；无法识别时返回 LoginRequired（偏保守，避免价格被过度公开）</returns>
        public static PriceVisibility ParseVisibility(string? value)
        {
            if (string.Equals(value, nameof(PriceVisibility.Public), StringComparison.OrdinalIgnoreCase))
                return PriceVisibility.Public;

            // 改动说明：无法识别的配置值一律按最保守的 LoginRequired 处理，防止价格被意外公开
            return PriceVisibility.LoginRequired;
        }
    }
}
