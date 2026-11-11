using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 轴承类型实体
    /// 代表轴承的技术分类，如深沟球轴承、角接触轴承等
    /// 对应接口：GET /api/bearing-types
    /// </summary>
    public class BearingType : BaseEntity
    {
        /// <summary>
        /// 轴承类型代码
        /// 如 DGBB（深沟球）、ACBB（角接触）、SRB（调心滚子）等
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// 轴承类型名称
        /// 如 "深沟球轴承"、"角接触球轴承"等
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 轴承类型描述
        /// 详细的类型说明，包括特点、用途等
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>英文名称（如 Deep Groove Ball Bearing）</summary>
        public string? EnglishName { get; private set; }

        /// <summary>结构特点（人工维护）</summary>
        public string? StructureFeatures { get; private set; }

        /// <summary>载荷特性（径向为主 / 可承受轴向 / 联合载荷等）</summary>
        public string? LoadCharacteristics { get; private set; }

        /// <summary>典型型号前缀（如 60/62/63）</summary>
        public string? TypicalPrefixes { get; private set; }

        /// <summary>主要应用领域（逗号分隔）</summary>
        public string? MainApplications { get; private set; }

        /// <summary>是否可分离结构</summary>
        public bool? IsSeparable { get; private set; }

        /// <summary>结构示意图 URL</summary>
        public string? ImageUrl { get; private set; }

        /// <summary>
        /// 数据来源
        /// </summary>
        public DataSource? DataSource { get; private set; }

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private BearingType() { }

        /// <summary>
        /// 创建轴承类型
        /// </summary>
        /// <param name="code">类型代码</param>
        /// <param name="name">类型名称</param>
        /// <param name="description">类型描述（可选）</param>
        /// <exception cref="ArgumentException">当code或name为空时抛出</exception>
        public BearingType(string code, string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("类型代码不能为空", nameof(code));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("类型名称不能为空", nameof(name));

            Code = code;
            Name = name;
            Description = description;
        }

        /// <summary>
        /// 更新名称和描述
        /// </summary>
        public void Update(string? name, string? description)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name;
            if (description != null) Description = description;
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
        /// 更新类型资料（英文名、结构特点、载荷特性、典型前缀、主要应用、是否可分离、示意图）
        /// 改动说明（类型资料完善）：新增字段全部可空，Admin 保存时整表单覆盖，
        /// 人工维护后 DataSource 标记为 Manual，同步任务不会覆盖
        /// </summary>
        public void UpdateProfile(
            string? englishName,
            string? structureFeatures,
            string? loadCharacteristics,
            string? typicalPrefixes,
            string? mainApplications,
            bool? isSeparable,
            string? imageUrl)
        {
            EnglishName = englishName;
            StructureFeatures = structureFeatures;
            LoadCharacteristics = loadCharacteristics;
            TypicalPrefixes = typicalPrefixes;
            MainApplications = mainApplications;
            IsSeparable = isSeparable;
            ImageUrl = imageUrl;
            UpdateTimestamp();
        }
    }
}
