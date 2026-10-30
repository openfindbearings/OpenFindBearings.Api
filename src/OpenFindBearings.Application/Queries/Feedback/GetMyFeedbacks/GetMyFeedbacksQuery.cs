using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Feedback.GetMyFeedbacks
{
    /// <summary>
    /// 获取我的意见反馈列表查询（用户端）
    /// 只返回本人提交的记录，用于在"我的反馈"查看处理状态与管理员回复
    /// </summary>
    public class GetMyFeedbacksQuery : IRequest<PagedResult<MyFeedbackDto>>, IQuery
    {
        /// <summary>当前用户 ID（由端点按登录态填充）</summary>
        public Guid UserId { get; set; }

        /// <summary>页码</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页条数</summary>
        public int PageSize { get; set; } = 20;
    }
}