using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetCorrectionDetail
{
    /// <summary>
    /// 获取单个纠错详情查询处理器
    /// </summary>
    public class GetCorrectionDetailQueryHandler : IRequestHandler<GetCorrectionDetailQuery, CorrectionDto?>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetCorrectionDetailQueryHandler> _logger;

        public GetCorrectionDetailQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ILogger<GetCorrectionDetailQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CorrectionDto?> Handle(GetCorrectionDetailQuery request, CancellationToken cancellationToken)
        {
            var correction = await _correctionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (correction == null)
                return null;

            return correction?.ToDto();
        }
    }
}
