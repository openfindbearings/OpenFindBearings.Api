namespace OpenFindBearings.Application.Features.SystemConfig.DTOs
{
    /// <summary>
    /// 价格配置DTO
    /// </summary>
    public class PriceConfigDto
    {
        /// <summary>
        /// 默认价格可见性
        /// </summary>
        public string DefaultVisibility { get; set; } = "LoginRequired";

        /// <summary>
        /// 是否显示议价标签
        /// </summary>
        public bool ShowNegotiableLabel { get; set; } = true;

        /// <summary>
        /// 是否启用数值化价格用于排序
        /// </summary>
        public bool NumericForSorting { get; set; } = true;

        /// <summary>
        /// 价格提取正则表达式
        /// </summary>
        public string ExtractPattern { get; set; } = @"¥(\d+(?:\.\d+)?)";
    }
}
