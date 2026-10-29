using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetFeedbacks
{
    /// <summary>
    /// 获取意见反馈列表查询（管理端）
    /// 支持按状态、分类与关键词筛选，返回带提交人昵称与脱敏手机号的分页结果
    /// </summary>
    public class GetFeedbacksQuery : IRequest<PagedResult<FeedbackDto>>, IQuery
    {
        /// <summary>处理状态筛选（空=全部）</summary>
        public FeedbackStatus? Status { get; set; }

        /// <summary>分类筛选（空=全部）</summary>
        public FeedbackCategory? Category { get; set; }

        /// <summary>关键词（匹配反馈内容或联系方式）</summary>
        public string? Keyword { get; set; }

        /// <summary>页码</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页条数</summary>
        public int PageSize { get; set; } = 20;
    }
}