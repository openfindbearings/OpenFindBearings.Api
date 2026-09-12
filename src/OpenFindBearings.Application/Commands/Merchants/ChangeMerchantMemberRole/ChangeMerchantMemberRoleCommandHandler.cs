using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.ChangeMerchantMemberRole
{
    /// <summary>
    /// 变更商户成员角色命令处理器
    /// 守卫：操作人须为在职管理员；不能变更自己；降级最后一名在职管理员被拦截
    /// </summary>
    public class ChangeMerchantMemberRoleCommandHandler : IRequestHandler<ChangeMerchantMemberRoleCommand>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<ChangeMerchantMemberRoleCommandHandler> _logger;

        public ChangeMerchantMemberRoleCommandHandler(
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<ChangeMerchantMemberRoleCommandHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task Handle(ChangeMerchantMemberRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("变更成员角色: UserId={UserId}, MerchantId={MerchantId}, Role={Role}, OperatorId={OperatorId}",
                request.UserId, request.MerchantId, request.Role, request.OperatorId);

            if (request.Role != MerchantMember.RoleMerchantAdmin && request.Role != MerchantMember.RoleMerchantStaff)
            {
                throw new InvalidOperationException("角色必须为 MerchantAdmin 或 MerchantStaff");
            }

            var operatorMember = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorId, request.MerchantId, cancellationToken);
            if (operatorMember == null || !operatorMember.IsAdmin)
            {
                throw new UnauthorizedAccessException("需要商户管理员权限");
            }

            if (request.UserId == request.OperatorId)
            {
                throw new InvalidOperationException("不能变更自己的角色，请由其他管理员操作");
            }

            var target = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (target == null)
            {
                throw new InvalidOperationException("目标成员不是该商户在职成员");
            }

            // ≥1 管理员守卫：降级最后一名在职管理员被拦截
            if (target.IsAdmin && request.Role != MerchantMember.RoleMerchantAdmin)
            {
                var adminCount = await _merchantMemberRepository.CountActiveAdminsAsync(request.MerchantId, cancellationToken);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("商户至少需要一名在职管理员，请先指定新的管理员");
                }
            }

            target.ChangeRole(request.Role);
            await _merchantMemberRepository.UpdateAsync(target, cancellationToken);

            _logger.LogInformation("成员角色已变更: UserId={UserId}, Role={Role}", request.UserId, request.Role);
        }
    }
}
