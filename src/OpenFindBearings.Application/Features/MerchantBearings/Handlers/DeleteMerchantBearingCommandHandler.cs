using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 删除商家-轴承关联命令处理器
    /// </summary>
    public class DeleteMerchantBearingCommandHandler : IRequestHandler<DeleteMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<DeleteMerchantBearingCommandHandler> _logger;

        public DeleteMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<DeleteMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(
            DeleteMerchantBearingCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始删除商家-轴承关联: Id={Id}", request.Id);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.Id}");
            }

            await _merchantBearingRepository.DeleteAsync(request.Id, cancellationToken);

            _logger.LogInformation("商家-轴承关联删除成功: Id={Id}", request.Id);
        }
    }
}
