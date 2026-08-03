using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.RejectMerchant
{
    public class RejectMerchantCommandHandler : IRequestHandler<RejectMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<RejectMerchantCommandHandler> _logger;

        public RejectMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<RejectMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(RejectMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("拒绝商家认证: {MerchantId}, Reason={Reason}", request.Id, request.Reason);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            merchant.Reject(request.Reason);
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家认证已拒绝: {MerchantId}", request.Id);
        }
    }
}
