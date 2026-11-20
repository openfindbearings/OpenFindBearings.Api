using ToolGood.Words.Pinyin;

namespace OpenFindBearings.Domain.Services
{
    /// <summary>
    /// 商户名称拼音检索键生成器。
    /// 职责：把商户名称与公司全称转成三份检索键，供关键词模糊检索使用——
    /// 全拼（luoyangzhoucheng）、首字母（lyzc）、模糊归一全拼（luoyanzhouchen。
    /// 改动说明（2026-11-20）：商户认领/列表搜索原先只支持中文子串，用户输 "ly" 匹配不到
    ///   「洛阳…」；按主流做法在写入时预计算并存列，查询只做字符串匹配，避免每次搜索全表转换。
    /// 模糊归一只处理 zh/ch/sh 与 ang/eng/ing（方言 n/l 不分歧义大，暂不处理）。
    /// </summary>
    public static class PinyinSearchText
    {
        /// <summary>
        /// 生成检索键。参数可传多个字段（名称、公司全称），按空格拼接后统一转换。
        /// </summary>
        public static (string Pinyin, string Initials, string Fuzzy) Build(params string?[] parts)
        {
            // 改动说明（2026-11-20）：名称与公司全称常常完全相同，重复拼接会让检索键翻倍，
            //   故按去重后的字段列表生成（保留首次出现的顺序）
            var source = string.Join(
                " ",
                parts
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p!.Trim())
                    .Distinct(StringComparer.Ordinal));

            if (source.Length == 0)
            {
                return (string.Empty, string.Empty, string.Empty);
            }

            var pinyin = WordsHelper.GetPinyin(source, false).ToLowerInvariant();
            var initials = WordsHelper.GetFirstPinyin(source).ToLowerInvariant();

            var syllables = WordsHelper.GetPinyin(source, "|", false)
                .Split('|', StringSplitOptions.RemoveEmptyEntries);
            var fuzzy = string.Concat(syllables.Select(NormalizeSyllable));

            return (pinyin, initials, fuzzy);
        }

        /// <summary>
        /// 归一化用户输入，使其与 <see cref="Build"/> 产出的模糊键同一口径。
        /// 说明：输入无法切分音节，故按整串做同样的替换，与按音节归一的结果保持一致。
        /// </summary>
        public static string NormalizeQuery(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return string.Empty;
            }

            return keyword.Trim()
                .ToLowerInvariant()
                .Replace("zh", "z")
                .Replace("ch", "c")
                .Replace("sh", "s")
                .Replace("ang", "an")
                .Replace("eng", "en")
                .Replace("ing", "in");
        }

        /// <summary>
        /// 按音节做模糊归一：翘舌音并平舌音，后鼻音并前鼻音。
        /// </summary>
        private static string NormalizeSyllable(string syllable)
        {
            var current = syllable.ToLowerInvariant();

            if (current.StartsWith("zh", StringComparison.Ordinal))
            {
                current = "z" + current[2..];
            }
            else if (current.StartsWith("ch", StringComparison.Ordinal))
            {
                current = "c" + current[2..];
            }
            else if (current.StartsWith("sh", StringComparison.Ordinal))
            {
                current = "s" + current[2..];
            }

            return current
                .Replace("ang", "an")
                .Replace("eng", "en")
                .Replace("ing", "in");
        }
    }
}