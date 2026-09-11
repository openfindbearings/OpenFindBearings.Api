using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.History.DeleteMerchantHistory
{
    /// <summary>
    /// 删除单条商家浏览历史处理器：以 userId+merchantId 收敛归属后再删，防越权
    /// </summary>
    public class DeleteMerchantHistoryCommandHandler : IRequestHandler<DeleteMerchantHistoryCommand>
    {
        private readonly IUserMerchantHistoryRepository _merchantHistoryRepository;
        private readonly ILogger<DeleteMerchantHistoryCommandHandler> _logger;

        public DeleteMerchantHistoryCommandHandler(
            IUserMerchantHistoryRepository merchantHistoryRepository,
            ILogger<DeleteMerchantHistoryCommandHandler> logger)
        {
            _merchantHistoryRepository = merchantHistoryRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteMerchantHistoryCommand request, CancellationToken cancellationToken)
        {
            var history = await _merchantHistoryRepository.GetAsync(request.UserId, request.MerchantId, cancellationToken);
            if (history == null)
            {
                _logger.LogInformation("删除商家浏览历史：记录不存在 UserId={UserId} MerchantId={MerchantId}",
                    request.UserId, request.MerchantId);
                return;
            }

            await _merchantHistoryRepository.DeleteAsync(history.Id, cancellationToken);
            _logger.LogInformation("删除商家浏览历史成功: UserId={UserId} MerchantId={MerchantId}",
                request.UserId, request.MerchantId);
        }
    }
}
