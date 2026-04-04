using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 移除员工命令处理器
    /// </summary>
    public class RemoveStaffCommandHandler : IRequestHandler<RemoveStaffCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RemoveStaffCommandHandler> _logger;

        public RemoveStaffCommandHandler(
            IUserRepository userRepository,
            ILogger<RemoveStaffCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(RemoveStaffCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("移除员工: UserId={UserId}, OperatorId={OperatorId}",
                request.UserId, request.OperatorId);

            var operator_ = await _userRepository.GetByIdAsync(request.OperatorId, cancellationToken);
            var targetUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (operator_ == null || targetUser == null)
            {
                throw new InvalidOperationException("用户不存在");
            }

            // 检查操作人权限
            if (operator_.MerchantId != targetUser.MerchantId)
            {
                throw new UnauthorizedAccessException("您无权移除该员工");
            }

            targetUser.RemoveFromMerchant();
            await _userRepository.UpdateAsync(targetUser, cancellationToken);
        }
    }
}
