using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 品牌实体
    /// 代表轴承品牌，如 SKF、FAG、NSK 等
    /// </summary>
    public class Brand : BaseEntity
    {
        /// <summary>
        /// 品牌代码（如 SKF、FAG、NSK、HRB）
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 品牌所属国家
        /// </summary>
        public string? Country { get; private set; }

        /// <summary>
        /// 品牌Logo图片URL
        /// </summary>
        public string? LogoUrl { get; private set; }

        /// <summary>
        /// 品牌档次（国际一线、国际标准、国产一线等）
        /// </summary>
        public BrandLevel Level { get; private set; }

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private Brand() { }

        /// <summary>
        /// 创建新品牌
        /// </summary>
        /// <param name="code">品牌代码</param>
        /// <param name="name">品牌名称</param>
        /// <param name="level">品牌档次</param>
        public Brand(string code, string name, BrandLevel level)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("品牌代码不能为空", nameof(code));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("品牌名称不能为空", nameof(name));

            Code = code;
            Name = name;
            Level = level;
        }

        /// <summary>
        /// 更新品牌详细信息
        /// </summary>
        /// <param name="country">国家</param>
        /// <param name="logoUrl">Logo图片URL</param>
        public void UpdateDetails(string? country, string? logoUrl)
        {
            Country = country;
            LogoUrl = logoUrl;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新品牌档次
        /// </summary>
        /// <param name="newLevel">新档次</param>
        public void UpdateLevel(BrandLevel newLevel)
        {
            Level = newLevel;
            UpdateTimestamp();
        }
    }
}
