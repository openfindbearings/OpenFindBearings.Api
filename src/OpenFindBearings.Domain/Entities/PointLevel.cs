using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户积分等级阈值（G7 等级/称号，v2.7.0）：按累计获得积分 TotalEarned 落档，
    /// 纯展示无特权（防高等级套利差）。Level 唯一递增，MinTotalEarned 为进入该档的最低累计分。
    /// 运营可在 Admin 调阈值表（进积分规则配置 tab），改后实时生效
    /// </summary>
    public class PointLevel : BaseEntity
    {
        /// <summary>等级（1 起连续递增）</summary>
        public int Level { get; private set; }

        /// <summary>进入该等级所需最低累计获得积分（TotalEarned >= 此值落档）</summary>
        public int MinTotalEarned { get; private set; }

        /// <summary>等级名称（如 青铜/白银/黄金；展示用）</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>是否启用（停用后该档不可达，自动落到下一启用档）</summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>EF 专用无参构造</summary>
        protected PointLevel() { }

        /// <summary>创建等级档</summary>
        public PointLevel(int level, int minTotalEarned, string name)
        {
            Level = level;
            MinTotalEarned = minTotalEarned;
            Name = name;
        }

        /// <summary>Admin 编辑（阈值/名称/启停）</summary>
        public void Update(int minTotalEarned, string name, bool enabled)
        {
            MinTotalEarned = minTotalEarned;
            Name = name;
            Enabled = enabled;
        }
    }
}