using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Notifications.GetUnreadCount
{
    /// <summary>
    /// 站内信未读数查询（TabBar 角标用）
    /// </summary>
    public record GetUnreadCountQuery : IRequest<int>, IQuery
    {
        /// <summary>收件人（当前登录用户）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>
    /// 未读数查询处理器
    /// </summary>
    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
    {
        private readonly INotificationRepository _notificationRepository;

        public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
        {
            return await _notificationRepository.CountUnreadAsync(request.UserId, cancellationToken);
        }
    }
}
