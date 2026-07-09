using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.SetPriceVisibility
{
    /// <summary>
    /// 设置价格可见性命令处理器
    /// </summary>
    public class SetPriceVisibilityCommandHandler : IRequestHandler<SetPriceVisibilityCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SetPriceVisibilityCommandHandler> _logger;

        public SetPriceVisibilityCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IUserRepository userRepository,
            ILogger<SetPriceVisibilityCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(SetPriceVisibilityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("设置价格可见性: MerchantBearingId={MerchantBearingId}, Visibility={Visibility}",
                request.MerchantBearingId, request.Visibility);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家产品不存在: {request.MerchantBearingId}");
            }

            // 所有权验证：当前用户必须属于该商家
            var currentUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (currentUser == null || currentUser.MerchantId != merchantBearing.MerchantId)
            {
                throw new UnauthorizedAccessException("无权修改其他商家的轴承信息");
            }

            merchantBearing.SetPriceVisibility(request.Visibility);
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);
        }
    }
}
