using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.CheckMerchantBearingExists
{
    /// <summary>
    /// 检查商家-轴承关联是否存在查询处理器
    /// </summary>
    public class CheckMerchantBearingExistsQueryHandler : IRequestHandler<CheckMerchantBearingExistsQuery, bool>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<CheckMerchantBearingExistsQueryHandler> _logger;

        public CheckMerchantBearingExistsQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<CheckMerchantBearingExistsQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            CheckMerchantBearingExistsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("检查商家-轴承关联是否存在: MerchantId={MerchantId}, BearingId={BearingId}",
                request.MerchantId, request.BearingId);

            return await _merchantBearingRepository.ExistsAsync(request.MerchantId, request.BearingId, cancellationToken);
        }
    }
}
