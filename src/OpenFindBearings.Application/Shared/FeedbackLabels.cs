using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Shared
{
    /// <summary>
    /// 意见反馈展示文案映射
    /// 管理端与用户端共用同一套中文口径，避免两处手写文案漂移
    /// </summary>
    public static class FeedbackLabels
    {
        /// <summary>
        /// 分类中文名
        /// </summary>
        public static string Category(FeedbackCategory category) => category switch
        {
            FeedbackCategory.Suggestion => "功能建议",
            FeedbackCategory.Bug => "问题反馈",
            FeedbackCategory.ContentReport => "内容举报",
            _ => "其他"
        };

        /// <summary>
        /// 处理状态中文名
        /// </summary>
        public static string Status(FeedbackStatus status) => status switch
        {
            FeedbackStatus.Processing => "处理中",
            FeedbackStatus.Resolved => "已处理",
            FeedbackStatus.Closed => "已关闭",
            _ => "待处理"
        };
    }
}