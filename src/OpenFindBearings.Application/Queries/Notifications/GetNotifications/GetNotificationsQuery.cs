using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Notifications.GetNotifications
{
    /// <summary>
    /// 站内信分页查询（收件人视角）
    /// </summary>
    public record GetNotificationsQuery : IRequest<PagedResult<NotificationDto>>, IQuery
    {
        /// <summary>收件人（当前登录用户）</summary>
        public Guid UserId { get; init; }

        /// <summary>仅未读</summary>
        public bool UnreadOnly { get; init; }

        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    /// <summary>
    /// 站内信分页查询处理器
    /// </summary>
    public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
    {
        private readonly INotificationRepository _notificationRepository;

        public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await _notificationRepository.GetByUserAsync(
                request.UserId, request.UnreadOnly, request.Page, request.PageSize, cancellationToken);

            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                BizType = n.BizType,
                BizId = n.BizId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            return new PagedResult<NotificationDto>(dtos, total, request.Page, request.PageSize);
        }
    }
}
