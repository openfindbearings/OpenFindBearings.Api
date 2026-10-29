using MediatR;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Shared;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetFeedbacks
{
    /// <summary>
    /// 获取意见反馈列表查询处理器（管理端）
    /// </summary>
    public class GetFeedbacksQueryHandler : IRequestHandler<GetFeedbacksQuery, PagedResult<FeedbackDto>>
    {
        private readonly IUserFeedbackRepository _feedbacks;

        /// <summary>
        /// 构造：注入反馈仓储
        /// </summary>
        public GetFeedbacksQueryHandler(IUserFeedbackRepository feedbacks)
        {
            _feedbacks = feedbacks;
        }

        /// <summary>
        /// 执行查询：分类与状态枚举转中文展示名，其余字段原样透出
        /// </summary>
        public async Task<PagedResult<FeedbackDto>> Handle(GetFeedbacksQuery request, CancellationToken cancellationToken)
        {
            var result = await _feedbacks.SearchAsync(new FeedbackSearchParams
            {
                Status = request.Status,
                Category = request.Category,
                Keyword = request.Keyword,
                Page = request.Page,
                PageSize = request.PageSize
            }, cancellationToken);

            var items = result.Items.Select(f => new FeedbackDto
            {
                Id = f.Id,
                UserId = f.UserId,
                UserNickname = f.UserNickname,
                UserMobile = f.UserMobile,
                Category = f.Category.ToString(),
                CategoryText = FeedbackLabels.Category(f.Category),
                Content = f.Content,
                ImageUrls = f.ImageUrls.ToList(),
                Contact = f.Contact,
                ClientInfo = f.ClientInfo,
                Status = f.Status.ToString(),
                StatusText = FeedbackLabels.Status(f.Status),
                AdminReply = f.AdminReply,
                RepliedAt = f.RepliedAt,
                CreatedAt = f.CreatedAt
            }).ToList();

            return new PagedResult<FeedbackDto>(items, result.TotalCount, result.Page, result.PageSize);
        }
    }
}