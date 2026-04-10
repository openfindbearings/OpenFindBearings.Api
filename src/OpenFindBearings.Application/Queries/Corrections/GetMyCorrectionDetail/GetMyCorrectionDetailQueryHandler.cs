using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetMyCorrectionDetail
{
    /// <summary>
    /// 获取我的单条纠错详情查询处理器
    /// </summary>
    public class GetMyCorrectionDetailQueryHandler : IRequestHandler<GetMyCorrectionDetailQuery, CorrectionDto?>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetMyCorrectionDetailQueryHandler> _logger;

        public GetMyCorrectionDetailQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetMyCorrectionDetailQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<CorrectionDto?> Handle(GetMyCorrectionDetailQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取我的纠错详情: CorrectionId={CorrectionId}, UserId={UserId}",
                request.CorrectionId, request.UserId);

            var correction = await _correctionRepository.GetByIdAsync(request.CorrectionId, cancellationToken);
            if (correction == null)
            {
                _logger.LogWarning("纠错不存在: CorrectionId={CorrectionId}", request.CorrectionId);
                return null;
            }

            // 验证是否是当前用户提交的纠错
            if (correction.SubmittedBy != request.UserId)
            {
                _logger.LogWarning("用户无权查看此纠错: UserId={UserId}, CorrectionId={CorrectionId}",
                    request.UserId, request.CorrectionId);
                return null;
            }

            return correction.ToDto();
        }

        /// <summary>
        /// 获取目标显示名称
        /// </summary>
        private async Task<string> GetTargetDisplayAsync(string targetType, Guid targetId, CancellationToken cancellationToken)
        {
            if (targetType == "Bearing")
            {
                var bearing = await _bearingRepository.GetByIdAsync(targetId, cancellationToken);
                if (bearing != null)
                {
                    return $"{bearing.CurrentCode} - {bearing.Name}";
                }
            }
            else if (targetType == "Merchant")
            {
                var merchant = await _merchantRepository.GetByIdAsync(targetId, cancellationToken);
                if (merchant != null)
                {
                    return merchant.Name;
                }
            }
            return targetId.ToString();
        }
    }
}
