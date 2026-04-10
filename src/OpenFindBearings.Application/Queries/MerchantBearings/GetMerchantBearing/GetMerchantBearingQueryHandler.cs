using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearing
{
    public class GetMerchantBearingQueryHandler : IRequestHandler<GetMerchantBearingQuery, MerchantBearingDto?>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingQueryHandler> _logger;

        public GetMerchantBearingQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<MerchantBearingDto?> Handle(
            GetMerchantBearingQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家-轴承关联详情: Id={Id}", request.Id);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken);

            if (merchantBearing == null)
            {
                _logger.LogWarning("商家-轴承关联不存在: Id={Id}", request.Id);
                return null;
            }

            return merchantBearing.ToDto(request.IsAuthenticated);
        }
    }
}
