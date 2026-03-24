using OpenFindBearings.Domain.Common;

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
    }
}
