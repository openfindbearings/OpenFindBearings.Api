namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商家联系方式 DTO（v1.6.0 电话单轨）。
    /// 仅在本商户在职成员查看自家商户资料（信息维护 / 商户工作台）时随详情下发；
    /// 公共查询面一律不下发——号码只在寻货撮合达成后互给。
    /// </summary>
    public class MerchantContactDto
    {
        /// <summary>联系人姓名</summary>
        public string? ContactPerson { get; set; }

        /// <summary>固定电话</summary>
        public string? Phone { get; set; }

        /// <summary>手机号码</summary>
        public string? Mobile { get; set; }

        /// <summary>电子邮箱</summary>
        public string? Email { get; set; }

        /// <summary>地址</summary>
        public string? Address { get; set; }
    }
}