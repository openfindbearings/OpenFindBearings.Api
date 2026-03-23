using MediatR;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Application.Features.Users.Queries;
using System.Security.Claims;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 用户上下文中间件
    /// 从JWT中提取用户信息，获取或创建本地用户
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

            if (!string.IsNullOrEmpty(authUserId))
            {
                // 普通用户认证
                try
                {
                    var user = await mediator.Send(new GetUserByAuthIdQuery { AuthUserId = authUserId });

                    if (user == null)
                    {
                        // 首次登录，自动创建业务用户
                        var createCommand = new CreateUserFromAuthCommand
                        {
                            AuthUserId = authUserId,
                            UserType = Domain.Enums.UserType.Individual,
                            Nickname = context.User?.FindFirst(ClaimTypes.Name)?.Value
                        };
                        var userId = await mediator.Send(createCommand);
                        context.Items["UserId"] = userId;
                        context.Items["UserType"] = Domain.Enums.UserType.Individual.ToString();
                    }
                    else
                    {
                        context.Items["UserId"] = user.Id;
                        context.Items["UserType"] = user.UserType;
                    }
                    context.Items["AuthUserId"] = authUserId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取用户信息失败: AuthUserId={AuthUserId}", authUserId);
                }
            }
            else if (!string.IsNullOrEmpty(clientId))
            {
                // 客户端认证（同步程序）
                context.Items["ClientId"] = clientId;
                context.Items["IsClient"] = true;
                _logger.LogDebug("客户端认证: ClientId={ClientId}", clientId);
            }
            else
            {
                // 游客
                var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
                if (!string.IsNullOrEmpty(sessionId))
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
                        }
                        else
                        {
                            context.Items["UserId"] = guestUser.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "创建/获取游客用户失败: SessionId={SessionId}", sessionId);
                    }
                }
            }

            await _next(context);
        }
    }
}
