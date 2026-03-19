using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 分配用户到商家命令处理器
    /// </summary>
    public class AssignUserToMerchantCommandHandler : IRequestHandler<AssignUserToMerchantCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<AssignUserToMerchantCommandHandler> _logger;

        public AssignUserToMerchantCommandHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            ILogger<AssignUserToMerchantCommandHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(AssignUserToMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("分配用户到商家: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            user.AssignToMerchant(request.MerchantId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("用户已分配到商家: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);
        }
    }
}
