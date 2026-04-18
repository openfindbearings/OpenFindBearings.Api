using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 联系方式值对象
    /// 封装商家的公开联系信息，作为值对象使用
    /// </summary>
    public class ContactInfo : ValueObject
    {
        /// <summary>
        /// 联系人姓名
        /// </summary>
        public string? ContactPerson { get; private set; }

        /// <summary>
        /// 固定电话
        /// </summary>
        public string? Phone { get; private set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string? Mobile { get; private set; }

        /// <summary>
        /// QQ号码
        /// </summary>
        public string? QQ { get; private set; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        public string? Email { get; private set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; private set; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private ContactInfo() { }

        /// <summary>
        /// 创建联系方式值对象
        /// </summary>
        /// <param name="contactPerson">联系人姓名</param>
        /// <param name="phone">固定电话</param>
        /// <param name="mobile">手机号码</param>
        /// <param name="qq">QQ号码</param>
        /// <param name="email">电子邮箱</param>
        /// <param name="address">地址</param>
        public ContactInfo(
            string? contactPerson = null,
            string? phone = null,
            string? mobile = null,
            string? qq = null,
            string? email = null,
            string? address = null)
        {
            ContactPerson = contactPerson;
            Phone = phone;
            Mobile = mobile;
            QQ = qq;
            Email = email;
            Address = address;
        }

        /// <summary>
        /// 获取用于比较的组件列表
        /// 所有字段都用于判断两个值对象是否相等
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ContactPerson ?? string.Empty;
            yield return Phone ?? string.Empty;
            yield return Mobile ?? string.Empty;
            yield return QQ ?? string.Empty;
            yield return Email ?? string.Empty;
            yield return Address ?? string.Empty;
        }

        /// <summary>
        /// 判断是否有任何联系方式
        /// </summary>
        public bool HasAnyContact =>
            !string.IsNullOrWhiteSpace(ContactPerson) ||
            !string.IsNullOrWhiteSpace(Phone) ||
            !string.IsNullOrWhiteSpace(Mobile) ||
            !string.IsNullOrWhiteSpace(QQ) ||
            !string.IsNullOrWhiteSpace(Email) ||
            !string.IsNullOrWhiteSpace(Address);


        /// <summary>
        /// 城市（从地址提取）
        /// </summary>
        public string? City
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Address))
                    return null;

                var parts = Address.Split(new[] { '省', '市', '区', '县' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    var city = parts[1].Trim();
                    if (city.Length > 10) city = city.Substring(0, 10);
                    return city;
                }

                return Address.Length > 6 ? Address.Substring(0, 6) : Address;
            }
        }
    }
}
