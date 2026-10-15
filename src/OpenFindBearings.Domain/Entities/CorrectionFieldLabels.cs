using System;
using System.Collections.Generic;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 纠错字段显示名唯一口径（v2.13.2 收敛）：此前字段标签分散在三处且互不一致——
    /// 列表映射 CorrectionExtensions（PascalCase 键，与库中 camelCase 字段名不匹配恒回退原值）、
    /// 审计日志 CorrectionRequest.GetFieldDisplayName（PascalCase switch）、可纠错字段清单
    /// GetCorrectableFieldsQueryHandler（camelCase，含全部 15+9 项）。此处以"目标类型 + 字段 id"
    /// 为键统一，大小写不敏感，三处共用。
    /// </summary>
    public static class CorrectionFieldLabels
    {
        /// <summary>轴承字段标签（键为 camelCase 字段 id）</summary>
        private static readonly Dictionary<string, string> BearingMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "partNumber", "型号" },
            { "oldNumber", "型号（老型号）" },
            { "name", "名称" },
            { "description", "描述" },
            { "weight", "重量" },
            { "innerDiameter", "内径" },
            { "outerDiameter", "外径" },
            { "width", "宽度" },
            { "precisionGrade", "精度等级" },
            { "material", "材质" },
            { "sealType", "密封形式" },
            { "cageType", "保持架类型" },
            { "dynamicLoad", "动载荷" },
            { "staticLoad", "静载荷" },
            { "limitingSpeed", "极限转速" },
            { "limitingSpeedGrease", "极限转速（脂润滑）" },
            { "limitingSpeedOil", "极限转速（油润滑）" }
        };

        /// <summary>商家字段标签（键为 camelCase 字段 id）</summary>
        private static readonly Dictionary<string, string> MerchantMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "name", "商家名称" },
            { "companyName", "公司全称" },
            { "description", "简介" },
            { "businessScope", "经营范围" },
            { "contactPerson", "联系人" },
            { "phone", "电话" },
            { "mobile", "手机" },
            { "email", "邮箱" },
            { "address", "地址" }
        };

        /// <summary>
        /// 按目标类型（Bearing/Merchant，大小写不敏感）与字段 id 取显示名；未登记回退原字段 id。
        /// </summary>
        /// <param name="targetType">纠错目标类型（Bearing/Merchant）</param>
        /// <param name="fieldName">字段 id（camelCase）</param>
        public static string Label(string? targetType, string? fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return string.Empty;
            var map = string.Equals(targetType, "Merchant", StringComparison.OrdinalIgnoreCase)
                ? MerchantMap
                : BearingMap;
            return map.TryGetValue(fieldName, out var label) ? label : fieldName;
        }
    }
}