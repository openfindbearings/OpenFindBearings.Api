namespace OpenFindBearings.Application.Shared
{
    /// <summary>
    /// 联系方式脱敏工具（v2.22.3 互联网数据展示）。
    /// 口径：互联网来源商家的公开联系方式可展示，但手机号必须脱敏（防骚扰、防批量抓取），
    /// 固定电话属公开商户信息，原样展示；认证商家与非互联网来源商家不适用（仍走寻货撮合互给）。
    /// </summary>
    public static class ContactMasker
    {
        /// <summary>
        /// 手机号脱敏：保留前 3 位与后 4 位，中间以 **** 替代。
        /// 位数不足（非标准手机号）时保守处理——仅保留前 2 位与后 2 位；过短（少于 5 位）原样返回。
        /// </summary>
        /// <param name="mobile">原始手机号（可含空格、连字符）</param>
        /// <returns>脱敏后的号码；入参为空时返回 null</returns>
        public static string? MaskMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return mobile;

            // 仅保留数字，避免原始格式中的分隔符影响位数判断与脱敏结果
            var digits = new string(mobile.Where(char.IsDigit).ToArray());
            if (digits.Length < 5)
                return mobile;

            if (digits.Length >= 8)
                return $"{digits[..3]}****{digits[^4..]}";

            return $"{digits[..2]}****{digits[^2..]}";
        }

        /// <summary>
        /// 固定电话原样返回（区号 + 号码属公开商户信息，无隐私风险）。
        /// 单独封装是为了让调用方语义清晰：电话不脱敏、手机必须脱敏。
        /// </summary>
        /// <param name="phone">固定电话</param>
        /// <returns>原值</returns>
        public static string? KeepLandline(string? phone) => phone;
    }
}
