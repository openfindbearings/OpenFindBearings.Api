using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 品牌实体
    /// 代表轴承品牌，如 SKF、FAG、NSK 等
    /// 对应接口：GET /api/brands
    /// </summary>
    public class Brand : BaseEntity
    {
        /// <summary>
        /// 品牌代码（如 SKF、FAG、NSK、HRB）
        /// 通常使用品牌的官方缩写
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// 品牌名称
        /// 品牌的完整名称
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

        /// <summary>品牌简介/特点（人工维护，爬虫不覆盖）</summary>
        public string? Description { get; private set; }

        /// <summary>成立年份</summary>
        public int? FoundedYear { get; private set; }

        /// <summary>母公司或所属集团</summary>
        public string? ParentCompany { get; private set; }

        /// <summary>官网地址</summary>
        public string? Website { get; private set; }

        /// <summary>优势产品线（自由文本）</summary>
        public string? Advantages { get; private set; }

        /// <summary>型号后缀体系说明（如 SKF 2RS1=接触密封、ECP=玻纤保持架）</summary>
        public string? SuffixNotes { get; private set; }

        /// <summary>主要应用领域（逗号分隔）</summary>
        public string? MainApplications { get; private set; }

        /// <summary>
        /// 品牌档次（国际一线、国际标准、国产一线等）
        /// 用于价格参考和筛选
        /// </summary>
        public BrandLevel Level { get; private set; }

        /// <summary>
        /// 数据来源
        /// </summary>
        public DataSource? DataSource { get; private set; }

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private Brand() { }

        /// <summary>
        /// 创建新品牌
        /// </summary>
        /// <param name="code">品牌代码，如 "SKF"</param>
        /// <param name="name">品牌名称，如 "SKF"</param>
        /// <param name="level">品牌档次</param>
        /// <exception cref="ArgumentException">当code或name为空时抛出</exception>
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

        /// <summary>
        /// 更新品牌名称
        /// </summary>
        public void UpdateName(string name)
        {
            Name = name;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置数据来源
        /// </summary>
        public void SetDataSource(DataSource dataSource)
        {
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新品牌资料（简介、成立年份、母公司、官网、优势产品、后缀体系、主要应用）
        /// 改动说明（品牌资料完善）：新增字段全部可空，Admin 保存时整表单覆盖，
        /// 人工维护后 DataSource 标记为 Manual，同步任务不会覆盖
        /// </summary>
        public void UpdateProfile(
            string? description,
            int? foundedYear,
            string? parentCompany,
            string? website,
            string? advantages,
            string? suffixNotes,
            string? mainApplications)
        {
            Description = description;
            FoundedYear = foundedYear;
            ParentCompany = parentCompany;
            Website = website;
            Advantages = advantages;
            SuffixNotes = suffixNotes;
            MainApplications = mainApplications;
            UpdateTimestamp();
        }
    }
}
