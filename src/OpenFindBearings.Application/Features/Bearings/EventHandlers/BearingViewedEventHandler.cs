using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
{
    /// <summary>
    /// 轴承查看事件处理器
    /// 用于统计热门产品、用户行为分析
    /// </summary>
    public class BearingViewedEventHandler : INotificationHandler<BearingViewedEvent>
    {
        private readonly ILogger<BearingViewedEventHandler> _logger;
        private readonly IBearingViewStatsService _statsService;

        public BearingViewedEventHandler(
            ILogger<BearingViewedEventHandler> logger,
            IBearingViewStatsService statsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        }

        public async Task Handle(BearingViewedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "轴承被查看: 型号={PartNumber}, 当前浏览次数={ViewCount}, 用户类型={UserType}",
                notification.PartNumber,
                notification.CurrentViewCount,
                notification.IsAuthenticated ? "登录用户" : "游客");

            try
            {
                // 记录浏览次数统计（异步处理，不影响主流程）
                await _statsService.RecordViewAsync(
                    bearingId: notification.BearingId,
                    userId: notification.UserId,
                    sessionId: notification.SessionId,
                    viewedAt: notification.ViewedAt,
                    cancellationToken: cancellationToken);

                // 如果是热门产品，可以更新缓存
                if (notification.CurrentViewCount % 100 == 0)  // 每100次查看更新一次热门缓存
                {
                    await UpdateHotProductsCacheAsync(notification.BearingId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录轴承查看统计失败: {BearingId}", notification.BearingId);
            }
        }

        private async Task UpdateHotProductsCacheAsync(Guid bearingId, CancellationToken cancellationToken)
        {
            // 可以在这里更新热门产品缓存
            // 实际项目中可能会通过定时任务计算热门产品，而不是实时计算
            await Task.CompletedTask;
        }
    }
}
