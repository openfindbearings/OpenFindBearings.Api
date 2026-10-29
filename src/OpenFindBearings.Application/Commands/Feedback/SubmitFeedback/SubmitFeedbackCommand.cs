using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Feedback.SubmitFeedback
{
    /// <summary>
    /// 提交意见反馈命令
    /// 图片为先行上传得到的相对媒体键，提交时只做前缀与张数校验
    /// </summary>
    public record SubmitFeedbackCommand : IRequest<Guid>, ICommand
    {
        /// <summary>提交人用户 ID</summary>
        public Guid UserId { get; init; }

        /// <summary>反馈分类</summary>
        public FeedbackCategory Category { get; init; }

        /// <summary>反馈内容（5 到 1000 字）</summary>
        public string Content { get; init; } = string.Empty;

        /// <summary>附件图片相对路径（最多 3 张）</summary>
        public List<string>? ImageUrls { get; init; }

        /// <summary>联系方式（可空）</summary>
        public string? Contact { get; init; }

        /// <summary>客户端信息（平台与版本，可空）</summary>
        public string? ClientInfo { get; init; }
    }
}