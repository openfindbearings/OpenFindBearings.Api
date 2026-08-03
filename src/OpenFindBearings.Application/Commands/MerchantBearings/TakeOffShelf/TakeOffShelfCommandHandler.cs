using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.TakeOffShelf
{
    /// <summary>
    /// 下架产品命令处理器
    /// </summary>
    public class TakeOffShelfCommandHandler : IRequestHandler<TakeOffShelfCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<TakeOffShelfCommandHandler> _logger;

        public TakeOffShelfCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IUserRepository userRepository,
            ILogger<TakeOffShelfCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(
            TakeOffShelfCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始下架产品: MerchantBearingId={MerchantBearingId}", request.MerchantBearingId);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            // 所有权验证：当前用户必须属于该商家
            var currentUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (currentUser == null || currentUser.MerchantId != merchantBearing.MerchantId)
            {
                throw new UnauthorizedAccessException("无权修改其他商家的轴承信息");
            }

            merchantBearing.TakeOffShelf();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("产品下架成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
