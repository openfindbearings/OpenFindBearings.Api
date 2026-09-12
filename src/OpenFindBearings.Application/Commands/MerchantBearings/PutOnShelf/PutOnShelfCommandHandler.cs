using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.PutOnShelf
{
    /// <summary>
    /// 上架产品命令处理器
    /// </summary>
    public class PutOnShelfCommandHandler : IRequestHandler<PutOnShelfCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<PutOnShelfCommandHandler> _logger;

        public PutOnShelfCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<PutOnShelfCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task Handle(
            PutOnShelfCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始上架产品: MerchantBearingId={MerchantBearingId}", request.MerchantBearingId);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            // 所有权验证：当前用户必须是该商户的在职成员（成员表判定，支持一人多商户）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
            {
                throw new UnauthorizedAccessException("无权修改其他商家的轴承信息");
            }

            merchantBearing.PutOnShelf();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("产品上架成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
