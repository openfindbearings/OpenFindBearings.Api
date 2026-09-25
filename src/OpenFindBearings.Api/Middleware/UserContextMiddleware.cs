using MediatR;
using OpenFindBearings.Application.Commands.Users.Commands;
using OpenFindBearings.Application.Commands.Users.CreateUserFromAuth;
using OpenFindBearings.Application.Commands.Users.SyncUserProfile;
using OpenFindBearings.Application.Commands.Users.MigrateGuestData;
using OpenFindBearings.Application.Queries.Users.GetUserByAuthId;
using OpenFindBearings.Application.Queries.Users.GetUserBySessionId;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
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

        public async Task InvokeAsync(HttpContext context, IMediator mediator, IMerchantMemberRepository memberRepository)
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
                await HandleAuthenticatedUserAsync(context, mediator, memberRepository, authUserId, sessionId);
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
        /// 昵称 claim 兜底链（v2.11.1）：Identity 用户可能未设昵称（Name claim 空），
        /// 依次退到 preferred_username（登录名）→ 手机号，保证业务库昵称列可展示
        /// </summary>
        private static string? ResolveNicknameClaim(ClaimsPrincipal? user) =>
            user?.FindFirst(ClaimTypes.Name)?.Value
            ?? user?.FindFirst("preferred_username")?.Value
            ?? user?.FindFirst("phone_number")?.Value;

        /// <summary>
        /// 处理正式用户
        /// </summary>
        private async Task HandleAuthenticatedUserAsync(
            HttpContext context,
            IMediator mediator,
            IMerchantMemberRepository memberRepository,
            string authUserId,
            string? sessionId)
        {
            try
            {
                var user = await mediator.Send(new GetUserByAuthIdQuery { AuthUserId = authUserId });
                Guid? resolvedUserId = null;
                // v1.32.0 积分：注册奖励判定标记（创建分支置 true，JIT 完成后按标记发分）
                var isNewUser = false;

                if (user == null)
                {
                    isNewUser = true;
                    // ✅ 修改：移除 UserType
                    var inviteCode = context.User?.FindFirst("invite_code")?.Value;

                    var createCommand = new CreateUserFromAuthCommand
                    {
                        AuthUserId = authUserId,
                        RegistrationSource = Domain.Enums.RegistrationSource.Web,
                        // 改动说明（v2.11.1）：昵称 claim 走兜底链（Name 可能为空——Identity 用户未设昵称）
                        Nickname = ResolveNicknameClaim(context.User),
                        InviteCode = inviteCode
                    };
                    var userId = await mediator.Send(createCommand);
                    context.Items["UserId"] = userId;
                    resolvedUserId = userId;
                    // 改动说明：此处原为写入 UserType 枚举值，但 UserType 枚举与 User 实体字段均已移除，
                    //           项目已改走 RBAC 角色体系，恢复原代码会编译失败。
                    //           新创建用户尚未分配角色，按普通登录用户处理
                    context.Items["UserType"] = RateLimitUserType.User;

                    // 解析当前商户上下文（新用户无成员关系，结果为空属正常）
                    await ResolveCurrentMerchantAsync(context, userId, memberRepository);

                    _logger.LogInformation("首次登录，创建业务用户: AuthUserId={AuthUserId}, UserId={UserId}", authUserId, userId);

                    // 自动迁移游客数据
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await MigrateGuestDataAsync(mediator, sessionId, userId);
                    }
                }
                else
                {
                    // 改动说明（v2.12.0）：已注销账户拒绝访问——Identity 侧登录已被禁用，
                    //   但存量 access token 在有效期内仍可达本 API，此处按软删标记拦截（401）
                    if (user.DeactivatedAt.HasValue)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "账户已注销" });
                        return;
                    }

                    context.Items["UserId"] = user.Id;
                    resolvedUserId = user.Id;

                    // 解析当前商户上下文（支持一人多商户：X-Merchant-Id 指定或缺省首个），
                    //   返回该用户是否有在职商户成员关系（v2.1.0 起限流分档改用成员表，不再读已废弃的 User.MerchantId）
                    var hasMerchantMembership = await ResolveCurrentMerchantAsync(context, user.Id, memberRepository);

                    // 改动说明：限流分档原依据 RBAC 角色 + User.MerchantId 推导；User.MerchantId 已废弃移除，
                    //           改为"平台管理员角色→Admin 档 / 有在职商户成员→Merchant 档 / 其余→User 档"，
                    //           商户身份一律以成员表为准（与"业务鉴权不信全局 role"的原则对齐）
                    context.Items["UserType"] = DeriveRateLimitUserType(user.Roles, hasMerchantMembership);

                    // 如果还有未迁移的游客数据，自动迁移
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await MigrateGuestDataAsync(mediator, sessionId, user.Id);
                    }
                }
                context.Items["AuthUserId"] = authUserId;

                // 改动说明（v2.11.0）：手机号缓存 JIT 回填——按 phone_number claim 同步 User.Mobile，
                //   供同商户成员详情展示；新建与既有用户统一在此处理（无 claim/同值时命令内部跳过）
                // 改动说明（v2.11.1）：扩展为资料同步（命令更名 SyncUserProfileCommand）——
                //   昵称也按兜底链回填（仅空值补写），修复建号时 Name claim 为空导致的成员详情"未命名"
                if (resolvedUserId.HasValue)
                {
                    await mediator.Send(new SyncUserProfileCommand
                    {
                        UserId = resolvedUserId.Value,
                        Mobile = context.User?.FindFirst("phone_number")?.Value,
                        Nickname = ResolveNicknameClaim(context.User)
                    });

                    // 改动说明（v1.32.0 积分底座）：赚端两场景挂 JIT 链路——
                    //   注册奖励=首次创建业务用户一次性；每日登录=当日首次请求
                    //   （bizId 含 UTC 日期天然幂等，同日后续请求被服务层跳过）。
                    //   GrantAsync 内部吞异常，积分失败不影响登录主流程
                    // 改动说明（v1.34.0 防刷）：注册奖励改走认领台账，键绑手机号而非 userId——
                    //   注销重注册 userId 会变、手机号不变（Identity 匿名化释放号码后同号可再注册），
                    //   台账保证同号一生只领一次 50 分；无手机号 claim 的账号（如后台账号）
                    //   回退 userId 键（非 App 刷分面）
                    var points = context.RequestServices.GetRequiredService<IPointsService>();
                    if (isNewUser)
                    {
                        var registerPhone = context.User?.FindFirst("phone_number")?.Value;
                        var registerKey = string.IsNullOrWhiteSpace(registerPhone)
                            ? $"user:{resolvedUserId.Value:N}:register"
                            : $"phone:{registerPhone}:register";
                        await points.GrantOneTimeAsync(resolvedUserId.Value, PointTransaction.TypeRegisterBonus,
                            registerKey, "新用户注册奖励");
                    }
                    await points.GrantAsync(resolvedUserId.Value, PointTransaction.TypeDailyLogin,
                        $"daily_login:{resolvedUserId.Value:N}:{DateTime.UtcNow:yyyyMMdd}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理正式用户失败: AuthUserId={AuthUserId}", authUserId);
            }
        }

        /// <summary>
        /// 解析当前商户上下文（CurrentMerchantId）
        /// 优先采用请求头 X-Merchant-Id（必须是该用户的在职成员商户，否则回退缺省）；
        /// 缺省取用户首个在职成员商户；无成员关系时为空
        /// 返回值：该用户是否为任一商户的在职成员（供限流分档使用）
        /// </summary>
        private async Task<bool> ResolveCurrentMerchantAsync(
            HttpContext context,
            Guid userId,
            IMerchantMemberRepository memberRepository)
        {
            try
            {
                var header = context.Request.Headers["X-Merchant-Id"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out var requestedMerchantId))
                {
                    var member = await memberRepository.GetActiveByUserAndMerchantAsync(userId, requestedMerchantId);
                    if (member != null)
                    {
                        context.Items["CurrentMerchantId"] = requestedMerchantId;
                        return true;
                    }
                }

                var members = await memberRepository.GetActiveByUserIdAsync(userId);
                if (members.Count > 0)
                {
                    context.Items["CurrentMerchantId"] = members[0].MerchantId;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析当前商户上下文失败: UserId={UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 依据平台级 RBAC 角色 + 商户成员关系推导限流用户类型
        /// </summary>
        /// <param name="roles">用户拥有的平台级角色名称集合</param>
        /// <param name="isMerchant">是否为任一商户的在职成员（成员表判定，非废弃的 User.MerchantId）</param>
        /// <returns>限流分档标识；无法匹配任何特殊身份时返回普通用户档位</returns>
        private static string DeriveRateLimitUserType(IReadOnlyList<string>? roles, bool isMerchant)
        {
            var hasRole = new Func<string, bool>(name =>
                roles != null && roles.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase)));

            // 平台管理员优先（仅识别全局 Admin/SuperAdmin；商户管理员不再是平台管理员档）
            if (hasRole("Admin") || hasRole("SuperAdmin"))
                return RateLimitUserType.Admin;

            // 商户成员（管理员/员工）走商户档，身份以成员表为准
            if (isMerchant)
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
