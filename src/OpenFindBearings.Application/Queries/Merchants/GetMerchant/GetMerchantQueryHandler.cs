using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchant
{
    /// <summary>
    /// 获取商家查询处理器
    /// </summary>
    public class GetMerchantQueryHandler : IRequestHandler<GetMerchantQuery, MerchantDetailDto?>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantQueryHandler> _logger;

        public GetMerchantQueryHandler(
            IMerchantRepository merchantRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<MerchantDetailDto?> Handle(GetMerchantQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家详情: MerchantId={MerchantId}, IsAuthenticated={IsAuthenticated}",
                request.Id, request.IsAuthenticated);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
                return null;

            // 获取商家在售产品列表
            var merchantBearings = await _merchantBearingRepository.GetOnSaleByMerchantAsync(request.Id, cancellationToken);

            var products = merchantBearings.Select(mb => mb.ToDto(request.IsAuthenticated)).ToList();

            return merchant.ToDetailDto(products, request.IsAuthenticated);
        }
    }
}
