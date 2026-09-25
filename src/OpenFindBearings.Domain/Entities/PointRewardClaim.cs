using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 一次性积分奖励认领台账（v1.34.0 防刷设计）：一行 = 某个"不变量键"（手机号/
    /// 统一社会信用代码）对某类一次性奖励的一次认领。BizKey 唯一索引即幂等防线——
    /// 注销重注册换 userId、删店重入驻换 merchantId 都绕不开号/照这两个不变量。
    /// 与积分流水的关键区别：流水是个人数据（注销即删），台账是平台防刷底账（永不删除，
    /// 仅存键与归属快照，不含可扩展 PII）
    /// </summary>
    public class PointRewardClaim : BaseEntity
    {
        /// <summary>幂等键（如 phone:13800000000:register、credit:91XX...:approved）</summary>
        public string BizKey { get; private set; } = string.Empty;

        /// <summary>奖励动作类型（PointTransaction.Type* 常量）</summary>
        public string GrantType { get; private set; } = string.Empty;

        /// <summary>首次认领时的用户 ID（仅审计溯源用，不参与任何查询关联）</summary>
        public Guid FirstClaimerUserId { get; private set; }

        /// <summary>EF 专用无参构造</summary>
        protected PointRewardClaim() { }

        /// <summary>
        /// 创建台账认领记录
        /// </summary>
        /// <param name="bizKey">幂等键（号/照维度）</param>
        /// <param name="grantType">奖励动作类型</param>
        /// <param name="userId">认领人</param>
        public PointRewardClaim(string bizKey, string grantType, Guid userId)
        {
            BizKey = bizKey;
            GrantType = grantType;
            FirstClaimerUserId = userId;
        }
    }
}
