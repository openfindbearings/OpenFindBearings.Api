using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 轴承类型
    /// </summary>
    public class BearingType : BaseEntity
    {
        /// <summary>
        /// 轴承类型代码
        /// </summary>
        public string Code { get; private set; }
        /// <summary>
        /// 轴承类型名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 轴承类型描述
        /// </summary>
        public string? Description { get; private set; }

        private BearingType() { }

        public BearingType(string code, string name, string? description = null)
        {
            Code = code;
            Name = name;
            Description = description;
        }
    }
}
