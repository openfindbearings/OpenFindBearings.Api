using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.Features.Permissions.Commands
{
    /// <summary>
    /// 更新权限命令
    /// </summary>
    public record UpdatePermissionCommand : IRequest
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; init; }
    }
}
