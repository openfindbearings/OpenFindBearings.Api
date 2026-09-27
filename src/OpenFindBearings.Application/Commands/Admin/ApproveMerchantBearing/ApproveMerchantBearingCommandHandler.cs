using MediatR;
using OpenFindBearings.Application.Services;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Admin.ApproveMerchantBearing
{
    /// <summary>
    /// 审核通过商家产品命令处理器
    /// </summary>
    public class ApproveMerchantBearingCommandHandler : IRequestHandler<ApproveMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        // v2.5.0 商家经济：审核通过使商品转为在售，需重算商户商家等级
        private readonly IMerchantGradeService _merchantGrades;
        private readonly ILogger<ApproveMerchantBearingCommandHandler> _logger;

        public ApproveMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantGradeService grades,
            ILogger<ApproveMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantGrades = grades;
            _logger = logger;
        }

        public async Task Handle(ApproveMerchantBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过商家产品: MerchantBearingId={MerchantBearingId}, Reviewer={ReviewerId}",
                request.MerchantBearingId, request.ReviewedBy);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家产品不存在: {request.MerchantBearingId}");
            }

            merchantBearing.Approve();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);
            await _merchantGrades.RecomputeAsync(merchantBearing.MerchantId, cancellationToken);

            _logger.LogInformation("商家产品审核通过成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
