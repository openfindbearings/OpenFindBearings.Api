using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenFindBearings.Domain.Services
{
    /// <summary>
    /// 价格文本按比例调整工具（批量改价）
    /// 职责：从自由文本价格描述中找出金额数字并按百分比改写，数量、规格、折扣等非金额数字保持不变。
    /// 说明：价格在库中是自由文本（如 "¥55-60"、"28元/个"、"电议"），批量改价只能对数字部分做等比调整，
    ///       无法识别的文本一律返回失败，由调用方跳过并给出原因，避免改出错误报价。
    /// </summary>
    public static class PriceTextAdjuster
    {
        /// <summary>
        /// 数字识别正则：优先匹配带千分位的写法（1,200.50），其次匹配普通数字
        /// </summary>
        private static readonly Regex NumberRegex = new(
            @"\d{1,3}(?:,\d{3})+(?:\.\d+)?|\d+(?:\.\d+)?",
            RegexOptions.Compiled);

        /// <summary>
        /// 数字后面紧跟这些单位时视为数量、规格、时间或折扣，不参与调价
        /// </summary>
        private static readonly HashSet<char> NonPriceSuffixes =
        [
            '套', '个', '件', '支', '箱', '只', '条', '片', '对', '组',
            '台', '包', '桶', '斤', '吨', '天', '年', '月', '日', '折',
            '次', '家', '人', '米', '克', '起'
        ];

        /// <summary>
        /// 金额前缀符号
        /// </summary>
        private static readonly HashSet<char> CurrencyPrefixes = ['¥', '￥'];

        /// <summary>
        /// 金额后缀符号
        /// </summary>
        private static readonly HashSet<char> CurrencySuffixes = ['元', '块', '圆'];

        /// <summary>
        /// 调整后单项金额上限，超过视为录入异常
        /// </summary>
        private const decimal MaxAdjustedValue = 1_000_000_000m;

        /// <summary>
        /// 按百分比调整价格文本中的金额数字
        /// </summary>
        /// <param name="description">原价格描述文本</param>
        /// <param name="percent">调整幅度，正数为涨价、负数为降价</param>
        /// <param name="result">调整后的价格文本；失败时为原文</param>
        /// <param name="reason">失败原因，成功时为 null</param>
        /// <returns>识别到可调整金额并完成改写时返回 true</returns>
        public static bool TryAdjustByPercent(
            string? description,
            decimal percent,
            out string result,
            out string? reason)
        {
            result = description ?? string.Empty;
            reason = null;

            if (string.IsNullOrWhiteSpace(description))
            {
                reason = "价格描述为空";
                return false;
            }

            var matches = NumberRegex.Matches(description);
            if (matches.Count == 0)
            {
                reason = "价格里没有可识别的金额（如电议、面议）";
                return false;
            }

            var factor = 1m + percent / 100m;
            var isPrice = new bool[matches.Count];
            var priceCount = 0;

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var prefix = PreviousNonSpaceChar(description, match.Index);
                var suffix = NextNonSpaceChar(description, match.Index + match.Length);

                var hasCurrencyMarker =
                    (prefix.HasValue && CurrencyPrefixes.Contains(prefix.Value)) ||
                    (suffix.HasValue && CurrencySuffixes.Contains(suffix.Value));
                var hasNonPriceSuffix = suffix.HasValue && NonPriceSuffixes.Contains(suffix.Value);

                // 带货币符号的一定是金额；不带货币符号时，只要后面不是数量/规格单位，也按金额处理
                if (hasCurrencyMarker || !hasNonPriceSuffix)
                {
                    isPrice[i] = true;
                    priceCount++;
                }
            }

            if (priceCount == 0)
            {
                reason = "价格里的数字都是数量或规格，未找到金额";
                return false;
            }

            var builder = new StringBuilder(description.Length + 8);
            var cursor = 0;

            for (var i = 0; i < matches.Count; i++)
            {
                if (!isPrice[i])
                {
                    continue;
                }

                var match = matches[i];
                builder.Append(description, cursor, match.Index - cursor);

                var hasGrouping = match.Value.Contains(',');
                var raw = hasGrouping ? match.Value.Replace(",", string.Empty) : match.Value;
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                {
                    reason = "价格数值无法解析";
                    return false;
                }

                var adjusted = decimal.Round(value * factor, 2, MidpointRounding.AwayFromZero);
                if (adjusted <= 0 || adjusted > MaxAdjustedValue)
                {
                    reason = "调整后的价格超出合理范围";
                    return false;
                }

                builder.Append(adjusted.ToString(hasGrouping ? "#,##0.##" : "0.##", CultureInfo.InvariantCulture));
                cursor = match.Index + match.Length;
            }

            builder.Append(description, cursor, description.Length - cursor);
            result = builder.ToString();
            return true;
        }

        /// <summary>
        /// 取指定位置之前的第一个非空白字符（用于识别货币前缀）
        /// </summary>
        private static char? PreviousNonSpaceChar(string text, int index)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return text[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 取指定位置之后的第一个非空白字符（用于识别金额或数量单位）
        /// </summary>
        private static char? NextNonSpaceChar(string text, int index)
        {
            for (var i = index; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return text[i];
                }
            }

            return null;
        }
    }
}
