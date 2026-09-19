namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商户证照材料类型（v2.7.0 入驻材料分层：不同类型材料的必备性随商家类型而变）
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// 营业执照（全渠道必备，企业与个体户的准入凭证）
        /// </summary>
        BusinessLicense = 1,

        /// <summary>
        /// 品牌授权书（授权经销商必备，轴承行业防假冒授权的核心材料）
        /// </summary>
        BrandAuthorization = 2,

        /// <summary>
        /// 厂房照片（生产厂家选传，用于认证加权与可信展示）
        /// </summary>
        FactoryPhoto = 3
    }
}
