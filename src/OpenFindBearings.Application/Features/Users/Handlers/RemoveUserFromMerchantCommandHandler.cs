using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 从商家移除用户命令处理器
    /// </summary>
    public class RemoveUserFromMerchantCommandHandler : IRequestHandler<RemoveUserFromMerchantCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RemoveUserFromMerchantCommandHandler> _logger;

        public RemoveUserFromMerchantCommandHandler(
            IUserRepository userRepository,
            ILogger<RemoveUserFromMerchantCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(RemoveUserFromMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("从商家移除用户: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            if (user.MerchantId != request.MerchantId)
            {
                _logger.LogWarning("用户不属于该商家: UserId={UserId}, MerchantId={MerchantId}",
                    user.Id, request.MerchantId);
                return;
            }

            user.RemoveFromMerchant();
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("用户已从商家移除: UserId={UserId}", user.Id);
        }
    }
}
