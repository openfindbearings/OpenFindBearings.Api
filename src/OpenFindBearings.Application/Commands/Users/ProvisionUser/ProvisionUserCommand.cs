using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Users.ProvisionUser
{
    /// <summary>
    /// 预置业务用户命令（v1.38.0）：后台新建账号后其 API 业务库还没有 User 行
    /// （JIT 注册制，首次登录才建），导致平台角色分配 404；本命令按 Identity sub
    /// 提前建行并挂角色，与 JIT 的 GetOrCreate 语义兼容（行已存在则只做补角色）
    /// </summary>
    public record ProvisionUserCommand : IRequest<Guid>, ICommand
    {
        /// <summary>Identity 用户 ID（= JWT sub）</summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>用户名（建行时作为昵称兜底）</summary>
        public string? UserName { get; init; }

        /// <summary>要授予的平台角色名（可空=仅预置行不挂角色）</summary>
        public List<string> Roles { get; init; } = [];
    }
}
