namespace OpenFindBearings.Domain.Enums
{/// <summary>
 /// 注册来源
 /// </summary>
    public enum RegistrationSource
    {
        /// <summary>
        /// 微信小程序
        /// </summary>
        WeChat = 1,

        /// <summary>
        /// 手机号注册
        /// </summary>
        Mobile = 2,

        /// <summary>
        /// 网页版
        /// </summary>
        Web = 3,

        /// <summary>
        /// 后台创建
        /// </summary>
        Admin = 4,

        /// <summary>
        /// 游客
        /// </summary>
        Guest = 5
    }
}
