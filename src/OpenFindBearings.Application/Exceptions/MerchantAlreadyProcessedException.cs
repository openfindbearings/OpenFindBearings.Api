namespace OpenFindBearings.Application.Exceptions
{
    /// <summary>
    /// 商户申请已被处理异常：审核通过/拒绝时商户状态已不是 Pending（并发下另一管理员先处理了）。
    /// 由全局异常中间件映射为 HTTP 409 + code=MERCHANT_ALREADY_PROCESSED，前端据此提示并刷新列表。
    /// 改动说明：领域层 Merchant.Approve/Reject 已有状态守卫但抛 InvalidOperationException（映射 400），
    ///   并发冲突语义应为 409，故命令处理器先行显式检查并抛本异常，领域守卫保留作兜底。
    /// </summary>
    public class MerchantAlreadyProcessedException : Exception
    {
        /// <summary>冲突业务码，前端据此提示"该申请已被处理"</summary>
        public const string ErrorCode = "MERCHANT_ALREADY_PROCESSED";

        /// <summary>商户当前实际状态（供前端展示）</summary>
        public string CurrentStatus { get; }

        public MerchantAlreadyProcessedException(string currentStatus)
            : base($"该申请已被处理（当前状态：{currentStatus}），请刷新后查看")
        {
            CurrentStatus = currentStatus;
        }
    }
}
