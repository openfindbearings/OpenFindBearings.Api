using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 分配用户到商家命令
    /// </summary>
    public record AssignUserToMerchantCommand : IRequest
    {
        /// <summary>
        /// 用户认证ID
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 在商家的角色（可选）
        /// </summary>
        public string? MerchantRole { get; init; }
    }
}
