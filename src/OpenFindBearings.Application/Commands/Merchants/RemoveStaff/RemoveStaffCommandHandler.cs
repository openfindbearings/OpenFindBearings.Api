using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.RemoveStaff
{
    /// <summary>
    /// 移除员工命令处理器
    /// </summary>
    public class RemoveStaffCommandHandler : IRequestHandler<RemoveStaffCommand>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RemoveStaffCommandHandler> _logger;

        public RemoveStaffCommandHandler(
            IMerchantMemberRepository merchantMemberRepository,
            IUserRepository userRepository,
            ILogger<RemoveStaffCommandHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(RemoveStaffCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("移除员工: UserId={UserId}, MerchantId={MerchantId}, OperatorId={OperatorId}",
                request.UserId, request.MerchantId, request.OperatorId);

            // 改动说明：由 User.MerchantId 单值校验改为成员表校验操作人是该商户在职管理员
            var operatorMember = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorId, request.MerchantId, cancellationToken);
            if (operatorMember == null || !operatorMember.IsAdmin)
            {
                throw new UnauthorizedAccessException("您无权移除该员工");
            }

            // 目标必须是该商户的在职成员
            var targetMember = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (targetMember == null)
            {
                throw new InvalidOperationException("该用户不是此商户的员工");
            }

            // ≥1 管理员守卫：移除最后一名管理员被拦截，避免商户出现 0 管理员
            if (targetMember.IsAdmin)
            {
                var adminCount = await _merchantMemberRepository.CountActiveAdminsAsync(request.MerchantId, cancellationToken);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("商户至少需要一名在职管理员，请先添加新的管理员");
                }
            }

            targetMember.Remove();
            await _merchantMemberRepository.UpdateAsync(targetMember, cancellationToken);

            // 兼容遗留读取：若用户不再属于任何商户，清除 User.MerchantId 单值列
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user != null && user.MerchantId == request.MerchantId)
            {
                var remaining = await _merchantMemberRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
                if (remaining.Count == 0)
                {
                    user.RemoveFromMerchant();
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }
            }
        }
    }
}
