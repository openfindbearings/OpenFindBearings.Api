using MediatR;
using OpenFindBearings.Application.Commands.Users.Commands;
using OpenFindBearings.Application.Commands.Users.CreateUserFromAuth;
using OpenFindBearings.Application.Commands.Users.MigrateGuestData;
using OpenFindBearings.Application.Queries.Users.GetUserByAuthId;
using OpenFindBearings.Application.Queries.Users.GetUserBySessionId;
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
                // 改动说明：同步客户端（sync-client）没有 NameIdentifier 声明，限流中间件会把它
                //           判为未登录游客并按出口 IP 限流，与同 NAT 下的匿名流量共享配额。
                //           此处显式写入用户类型，使限流按客户端标识而非 IP 计数
                context.Items["UserType"] = RateLimitUserType.Merchant;
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
                    // ✅ 修改：移除 UserType
                    var inviteCode = context.User?.FindFirst("invite_code")?.Value;

                    var createCommand = new CreateUserFromAuthCommand
                    {
                        AuthUserId = authUserId,
                        RegistrationSource = Domain.Enums.RegistrationSource.Web,
                        Nickname = context.User?.FindFirst(ClaimTypes.Name)?.Value,
                        InviteCode = inviteCode
                    };
                    var userId = await mediator.Send(createCommand);
                    context.Items["UserId"] = userId;
                    // 改动说明：此处原为写入 UserType 枚举值，但 UserType 枚举与 User 实体字段均已移除，
                    //           项目已改走 RBAC 角色体系，恢复原代码会编译失败。
                    //           新创建用户尚未分配角色，按普通登录用户处理
                    context.Items["UserType"] = RateLimitUserType.User;

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

                    // 改动说明：原为 context.Items["UserType"] = user.UserType，因 UserType 枚举已移除而失效，
                    //           导致限流中间件读到的用户类型恒为 null，所有登录用户都被当作游客按 IP 限流。
                    //           改为依据 RBAC 角色推导限流用户类型，恢复 User / Premium 配额的可达性
                    context.Items["UserType"] = DeriveRateLimitUserType(user.Roles, user.MerchantId);

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
        /// 依据 RBAC 角色推导限流用户类型
        /// </summary>
        /// <param name="roles">用户拥有的角色名称集合</param>
        /// <param name="merchantId">用户关联的商户ID，为空表示非商户员工</param>
        /// <returns>限流分档标识；无法匹配任何特殊角色时返回普通用户档位</returns>
        private static string DeriveRateLimitUserType(IReadOnlyList<string>? roles, Guid? merchantId)
        {
            if (roles == null || roles.Count == 0)
                return RateLimitUserType.User;

            var hasRole = new Func<string, bool>(name =>
                roles.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase)));

            // 管理员优先。改动说明：MerchantAdmin 是种子数据中商户的默认角色
            //           （SeedData 里 merchant1/merchant2 均为 MerchantAdmin），
            //           若只识别 Admin/SuperAdmin，商户管理员会被错误降档到普通用户档
            if (hasRole("Admin") || hasRole("SuperAdmin") || hasRole("MerchantAdmin"))
                return RateLimitUserType.Admin;

            // 改动说明：MerchantAdmin 已在上面判为管理员，此处补充是为避免将来调整优先级后漏判
            if (merchantId.HasValue && (hasRole("MerchantStaff") || hasRole("MerchantAdmin")))
                return RateLimitUserType.Merchant;

            // 付费档位预留：当前无对应角色，统一按普通用户处理
            return RateLimitUserType.User;
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
