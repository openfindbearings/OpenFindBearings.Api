namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 意见反馈分类
    /// 用户提交反馈时必选，管理员按分类分诊处理
    /// </summary>
    public enum FeedbackCategory
    {
        /// <summary>功能建议</summary>
        Suggestion = 0,

        /// <summary>问题反馈（页面异常、数据错误等）</summary>
        Bug = 1,

        /// <summary>内容举报（违规商家、虚假信息等）</summary>
        ContentReport = 2,

        /// <summary>其他</summary>
        Other = 3
    }
}
