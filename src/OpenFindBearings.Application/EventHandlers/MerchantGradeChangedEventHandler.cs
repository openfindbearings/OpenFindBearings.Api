using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 商家等级变更事件处理器
    /// </summary>
    public class MerchantGradeChangedEventHandler : INotificationHandler<MerchantGradeChangedEvent>
    {
        private readonly ILogger<MerchantGradeChangedEventHandler> _logger;

        public MerchantGradeChangedEventHandler(ILogger<MerchantGradeChangedEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(MerchantGradeChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "商家等级变更: 商家ID={MerchantId}, 旧等级={OldGrade}, 新等级={NewGrade}",
                notification.MerchantId,
                notification.OldGrade,
                notification.NewGrade);

            _logger.LogInformation("商家等级变更待实现更新搜索权重和权益逻辑: MerchantId={MerchantId}", notification.MerchantId);
        }
    }
}
