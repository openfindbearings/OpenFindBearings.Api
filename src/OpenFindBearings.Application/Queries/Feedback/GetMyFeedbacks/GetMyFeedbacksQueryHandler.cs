using MediatR;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Shared;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Feedback.GetMyFeedbacks
{
    /// <summary>
    /// 获取我的意见反馈列表查询处理器（用户端）
    /// </summary>
    public class GetMyFeedbacksQueryHandler : IRequestHandler<GetMyFeedbacksQuery, PagedResult<MyFeedbackDto>>
    {
        private readonly IUserFeedbackRepository _feedbacks;

        /// <summary>
        /// 构造：注入反馈仓储
        /// </summary>
        public GetMyFeedbacksQueryHandler(IUserFeedbackRepository feedbacks)
        {
            _feedbacks = feedbacks;
        }

        /// <summary>
        /// 执行查询：按提交时间倒序分页，状态与分类转中文展示名
        /// </summary>
        public async Task<PagedResult<MyFeedbackDto>> Handle(GetMyFeedbacksQuery request, CancellationToken cancellationToken)
        {
            var result = await _feedbacks.SearchByUserAsync(
                request.UserId, request.Page, request.PageSize, cancellationToken);

            var items = result.Items.Select(f => new MyFeedbackDto
            {
                Id = f.Id,
                Category = f.Category.ToString(),
                CategoryText = FeedbackLabels.Category(f.Category),
                Content = f.Content,
                ImageUrls = f.ImageUrls.ToList(),
                Status = f.Status.ToString(),
                StatusText = FeedbackLabels.Status(f.Status),
                AdminReply = f.AdminReply,
                RepliedAt = f.RepliedAt,
                CreatedAt = f.CreatedAt
            }).ToList();

            return new PagedResult<MyFeedbackDto>(items, result.TotalCount, result.Page, result.PageSize);
        }
    }
}