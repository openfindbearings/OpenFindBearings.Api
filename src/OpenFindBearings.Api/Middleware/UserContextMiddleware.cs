using MediatR;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Application.Features.Users.Queries;
using System.Security.Claims;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 用户上下文中间件
    /// 从JWT中提取用户信息，自动创建业务用户，自动迁移游客数据
    /// </summary>
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserContextMiddleware> _logger;

        public UserContextMiddleware(
            RequestDelegate next,
            ILogger<UserContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IMediator mediator)
        {
            // 从JWT中获取用户认证ID
            var authUserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 检查是否是客户端认证
            var clientId = context.User?.FindFirst("client_id")?.Value;

            // 从请求头获取游客会话ID
            var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();

            // 情况1：正式用户（已登录）
            if (!string.IsNullOrEmpty(authUserId))
            {
                await HandleAuthenticatedUserAsync(context, mediator, authUserId, sessionId);
            }
            // 情况2：客户端认证（同步程序）
            else if (!string.IsNullOrEmpty(clientId))
            {
                context.Items["ClientId"] = clientId;
                context.Items["IsClient"] = true;
                _logger.LogDebug("客户端认证: ClientId={ClientId}", clientId);
            }
            // 情况3：游客（未登录）
            else if (!string.IsNullOrEmpty(sessionId))
            {
                await HandleGuestUserAsync(context, mediator, sessionId);
            }

            await _next(context);
        }

        /// <summary>
        /// 处理正式用户
        /// </summary>
        private async Task HandleAuthenticatedUserAsync(
            HttpContext context,
            IMediator mediator,
            string authUserId,
            string? sessionId)
        {
            try
            {
                var user = await mediator.Send(new GetUserByAuthIdQuery { AuthUserId = authUserId });

                if (user == null)
                {
                    // 首次登录，创建业务用户
                    var createCommand = new CreateUserFromAuthCommand
                    {
                        AuthUserId = authUserId,
                        UserType = Domain.Enums.UserType.Individual,
                        Nickname = context.User?.FindFirst(ClaimTypes.Name)?.Value
                    };
                    var userId = await mediator.Send(createCommand);
                    context.Items["UserId"] = userId;
                    context.Items["UserType"] = Domain.Enums.UserType.Individual.ToString();

                    _logger.LogInformation("首次登录，创建业务用户: AuthUserId={AuthUserId}, UserId={UserId}", authUserId, userId);

                    // 自动迁移游客数据
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await MigrateGuestDataAsync(mediator, sessionId, userId);
                    }
                }
                else
                {
                    context.Items["UserId"] = user.Id;
                    context.Items["UserType"] = user.UserType;

                    // 如果还有未迁移的游客数据，自动迁移
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await MigrateGuestDataAsync(mediator, sessionId, user.Id);
                    }
                }
                context.Items["AuthUserId"] = authUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理正式用户失败: AuthUserId={AuthUserId}", authUserId);
            }
        }

        /// <summary>
        /// 处理游客用户
        /// </summary>
        private async Task HandleGuestUserAsync(
            HttpContext context,
            IMediator mediator,
            string sessionId)
        {
            context.Items["SessionId"] = sessionId;
            context.Items["IsGuest"] = true;

            try
            {
                var guestUser = await mediator.Send(new GetUserBySessionIdQuery { SessionId = sessionId });
                if (guestUser == null)
                {
                    var createCommand = new CreateGuestUserCommand(sessionId);
                    var userId = await mediator.Send(createCommand);
                    context.Items["UserId"] = userId;
                    _logger.LogDebug("创建游客用户: SessionId={SessionId}, UserId={UserId}", sessionId, userId);
                }
                else
                {
                    context.Items["UserId"] = guestUser.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理游客用户失败: SessionId={SessionId}", sessionId);
            }
        }

        /// <summary>
        /// 迁移游客数据
        /// </summary>
        private async Task MigrateGuestDataAsync(IMediator mediator, string sessionId, Guid targetUserId)
        {
            try
            {
                var migrateCommand = new MigrateGuestDataCommand
                {
                    GuestSessionId = sessionId,
                    TargetUserId = targetUserId
                };
                await mediator.Send(migrateCommand);
                _logger.LogInformation("游客数据迁移完成: SessionId={SessionId}, TargetUserId={TargetUserId}", sessionId, targetUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "游客数据迁移失败: SessionId={SessionId}, TargetUserId={TargetUserId}", sessionId, targetUserId);
            }
        }
    }
}
