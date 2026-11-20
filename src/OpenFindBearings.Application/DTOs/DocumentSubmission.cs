using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 随入驻申请单提交的材料项（v2.7.0 材料分层：类型+文件URL，URL 由媒体上传通道先行换取）
    /// </summary>
    public record DocumentSubmission(DocumentType Type, string FileUrl);

    /// <summary>
    /// 入驻材料必备性矩阵（apply / resubmit / accept 三入口共用）：
    /// 全商家类型必备营业执照；授权经销商额外必备品牌授权书（轴承行业防假冒授权核心）；
    /// 生产厂家厂房照片选传；分销/贸易商执照即可。
    /// </summary>
    public static class DocumentRequirements
    {
        /// <summary>
        /// 按商家类型校验材料集合，返回缺失/超量提示语（通过时返回 null）
        /// </summary>
        public static string? Validate(MerchantType type, IReadOnlyList<DocumentSubmission>? documents)
        {
            var docs = documents ?? [];
            if (!docs.Any(d => d.Type == DocumentType.BusinessLicense && !string.IsNullOrWhiteSpace(d.FileUrl)))
            {
                return "必须上传营业执照";
            }
            if (type == MerchantType.AuthorizedDealer
                && !docs.Any(d => d.Type == DocumentType.BrandAuthorization && !string.IsNullOrWhiteSpace(d.FileUrl)))
            {
                return "授权经销商必须上传品牌授权书";
            }
            // 营业执照具有唯一性（一商户一主体）；授权书/厂房照允许多份（多品牌授权/多厂房）
            if (docs.Count(d => d.Type == DocumentType.BusinessLicense) > 1)
            {
                return "营业执照仅可提交一份";
            }
            return null;
        }

        /// <summary>
        /// 统一社会信用代码校验（v2.11.0 入驻三入口必填）：非空 + 18 位执照标准字符集
        /// （数字与大写字母，剔除易混 I/O/Z/S/V）+ GB 32100 校验位。
        /// 返回错误提示语，通过时返回 null。
        /// 改动说明：此前选填导致锁定后空值商户永远补不了、审核无法比对执照代码，
        ///   升必填；存量空值商户由 UpdateMerchant"一次性补录"通道放行。
        /// 改动说明（2026-11-20）：补校验位校验——18 位里第 18 位由前 17 位按权重算出，
        ///   纯本地即可拦住随手编造的号码，无需接入 OCR。
        /// </summary>
        public static string? ValidateCreditCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "统一社会信用代码不能为空（见营业执照）";
            var trimmed = code.Trim().ToUpperInvariant();
            if (trimmed.Length != 18 ||
                !System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[0-9A-HJ-NP-RT-UWXY]{18}$"))
                return "统一社会信用代码应为18位（营业执照上的数字与大写字母组合）";
            if (!HasValidCreditCodeChecksum(trimmed))
                return "统一社会信用代码校验位不正确，请核对营业执照上的号码";
            return null;
        }

        /// <summary>统一社会信用代码字符集（GB 32100，剔除 I/O/Z/S/V 后的 31 个字符）。</summary>
        private const string CreditCodeCharset = "0123456789ABCDEFGHJKLMNPQRTUWXY";

        /// <summary>前 17 位的加权因子（GB 32100 规定）。</summary>
        private static readonly int[] CreditCodeWeights =
            [1, 3, 9, 27, 19, 26, 16, 17, 20, 29, 25, 13, 8, 24, 10, 30, 28];

        /// <summary>
        /// 计算并比对第 18 位校验码：Σ(位值 × 权重) mod 31，再用 31 - 余数 取字符集下标（31 视为 0）。
        /// </summary>
        private static bool HasValidCreditCodeChecksum(string code)
        {
            var sum = 0;
            for (var i = 0; i < 17; i++)
            {
                var index = CreditCodeCharset.IndexOf(code[i]);
                if (index < 0)
                {
                    return false;
                }

                sum += index * CreditCodeWeights[i];
            }

            var checkIndex = (31 - sum % 31) % 31;
            return CreditCodeCharset[checkIndex] == code[17];
        }

        /// <summary>
        /// 该商家类型申请"认证"前必须已审核通过的材料集合（VerifyMerchant 口径）
        /// </summary>
        public static DocumentType[] RequiredTypes(MerchantType type)
            => type == MerchantType.AuthorizedDealer
                ? [DocumentType.BusinessLicense, DocumentType.BrandAuthorization]
                : [DocumentType.BusinessLicense];

        /// <summary>
        /// 材料类型中文名（错误提示与 DTO 展示共用）
        /// </summary>
        public static string DisplayName(DocumentType type) => type switch
        {
            DocumentType.BusinessLicense => "营业执照",
            DocumentType.BrandAuthorization => "品牌授权书",
            DocumentType.FactoryPhoto => "厂房照片",
            _ => type.ToString()
        };
    }
}
