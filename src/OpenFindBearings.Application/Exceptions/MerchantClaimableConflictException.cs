namespace OpenFindBearings.Application.Exceptions
{
    /// <summary>
    /// 商户认领冲突异常：真人自助新建时，库中已存在一个"未被认领且未认证"的同名/同信用代码商户。
    /// 语义不是错误而是"应改为认领"，携带已有商户信息供前端引导切换到认领流程。
    /// 由全局异常中间件映射为 HTTP 409 + code=MERCHANT_CLAIMABLE_EXISTS。
    /// </summary>
    public class MerchantClaimableConflictException : Exception
    {
        /// <summary>
        /// 冲突业务码，前端据此分支引导认领
        /// </summary>
        public const string ErrorCode = "MERCHANT_CLAIMABLE_EXISTS";

        /// <summary>
        /// 库中已存在且可认领的商户ID
        /// </summary>
        public Guid ExistingMerchantId { get; }

        /// <summary>
        /// 该商户名称（供前端弹窗展示"是否改为认领 X"）
        /// </summary>
        public string ExistingName { get; }

        public MerchantClaimableConflictException(Guid existingMerchantId, string existingName)
            : base("库中已存在可认领的同名商户")
        {
            ExistingMerchantId = existingMerchantId;
            ExistingName = existingName;
        }
    }
}
