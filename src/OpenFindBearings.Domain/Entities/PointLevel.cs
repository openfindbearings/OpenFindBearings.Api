using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户积分等级阈值（G7 等级/称号，v2.7.0）：按累计获得积分 TotalEarned 落档。
    /// Level 唯一递增，MinTotalEarned 为进入该档的最低累计分。
    /// 运营可在 Admin 调阈值表（"等级段位"tab），改后实时生效。
    /// 改动说明（v2.12.0 等级玩法）：档位改名王者荣耀风段位（青铜→最强王者）并新增
    /// LevelUpBonus 升档礼——跨档一次性发轴承币（每用户每档终身一次，幂等锚在认领台账），
    /// 特权仍只到"彩牌铭牌+升级礼"层面，不给倍率/额度（经济杠杆归商户等级通道，防双轨套利）
    /// </summary>
    public class PointLevel : BaseEntity
    {
        /// <summary>等级（1 起连续递增）</summary>
        public int Level { get; private set; }

        /// <summary>进入该等级所需最低累计获得积分（TotalEarned >= 此值落档）</summary>
        public int MinTotalEarned { get; private set; }

        /// <summary>等级名称（段位名，如 倔强青铜/最强王者；展示用）</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>是否启用（停用后该档不可达，自动落到下一启用档）</summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>升到该档的一次性升档礼轴承币（0=不发；Lv1 起步档恒 0）</summary>
        public int LevelUpBonus { get; private set; }

        /// <summary>EF 专用无参构造</summary>
        protected PointLevel() { }

        /// <summary>创建等级档</summary>
        public PointLevel(int level, int minTotalEarned, string name, int levelUpBonus = 0)
        {
            Level = level;
            MinTotalEarned = minTotalEarned;
            Name = name;
            LevelUpBonus = levelUpBonus;
        }

        /// <summary>Admin 编辑（阈值/名称/启停/升档礼）</summary>
        public void Update(int minTotalEarned, string name, bool enabled, int levelUpBonus)
        {
            MinTotalEarned = minTotalEarned;
            Name = name;
            Enabled = enabled;
            LevelUpBonus = levelUpBonus;
        }
    }
}