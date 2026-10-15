using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 通知服务实现：站内信落库（Notifications 表）+ 外部通道扇出（短信/微信/邮件，预留）。
    /// 改动说明（v1.6.0 多方式提醒）：在站内信之外新增 AddExternalAsync——按用户偏好把同一内容
    ///   扇出到启用的外部通道；未配置网关的通道（Sms/WeChat/Email:Enabled 未开）只记日志不外发，
    ///   使"接上网关即生效"而无需改调用方。事件订阅发生在 UnitOfWorkBehavior 提交之后，
    ///   故本服务在 Add 后显式 SaveChangesAsync 独立落库（通知写失败不回滚已成功的业务命令）。
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserPreferenceRepository _preferenceRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(
            ILogger<NotificationService> logger,
            INotificationRepository notificationRepository,
            IUserPreferenceRepository preferenceRepository,
            IUserRepository userRepository,
            IConfiguration configuration,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _notificationRepository = notificationRepository;
            _preferenceRepository = preferenceRepository;
            _userRepository = userRepository;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public async Task SendToAdminsAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            // TODO: 实现发送给管理员的通知
            // 可以是邮件、短信、站内信、钉钉等
            _logger.LogInformation("发送给管理员通知: {Title} - {Message}", title, message);
            await Task.CompletedTask;
        }

        public async Task SendToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
        {
            // 兼容旧调用：默认按系统类型落一条站内信
            await AddInAppAsync(userId, Notification.TypeSystem, title, message, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 写一条站内信并立即提交（订阅者时序在业务事务提交后，需独立 SaveChanges）
        /// </summary>
        public async Task AddInAppAsync(Guid userId, string type, string title, string body,
            string? bizType = null, Guid? bizId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _notificationRepository.AddAsync(
                    new Notification(userId, type, title, body, bizType, bizId), cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("站内信已送达: UserId={UserId}, Type={Type}, Title={Title}", userId, type, title);
            }
            catch (Exception ex)
            {
                // 通知失败只记录不影响业务链路（审核结果本身已落库）
                _logger.LogWarning(ex, "站内信写入失败: UserId={UserId}, Type={Type}", userId, type);
            }
        }

        /// <summary>
        /// 多渠道外发（v1.6.0）：按用户偏好把同一内容扇出到启用的外部通道；未配置网关的通道只记日志。
        /// 全程 best-effort，任何通道失败都不影响业务链路（与站内信同口径）。
        /// </summary>
        public async Task AddExternalAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default)
        {
            try
            {
                var pref = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);
                // 无偏好行=默认口径（与 UserPreference 默认值一致：短信关、微信/邮件开）
                var smsEnabled = pref?.SmsNotificationEnabled ?? false;
                var weChatEnabled = pref?.WeChatNotificationEnabled ?? true;
                var emailEnabled = pref?.EmailNotificationEnabled ?? true;

                if (!smsEnabled && !weChatEnabled && !emailEnabled)
                    return;

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

                // 网关就绪开关（默认全关=仅落日志，接上网关后打开 config 即生效）
                if (smsEnabled && IsChannelReady("Sms"))
                {
                    _logger.LogInformation("[外发-短信] UserId={UserId}, Phone={Phone}, Title={Title}",
                        userId, user?.Mobile, title);
                }
                if (weChatEnabled && IsChannelReady("WeChat"))
                {
                    _logger.LogInformation("[外发-微信] UserId={UserId}, Title={Title}", userId, title);
                }
                if (emailEnabled && IsChannelReady("Email"))
                {
                    _logger.LogInformation("[外发-邮件] UserId={UserId}, Body={Body}", userId, body);
                }

                // 改动说明（v1.6.0 预留）：通道网关未就绪时只记一条汇总日志，便于观测"待接网关"的多方式提醒量
                if (!IsChannelReady("Sms") && !IsChannelReady("WeChat") && !IsChannelReady("Email"))
                {
                    _logger.LogInformation("[外发-预留] 网关未就绪，多方式提醒暂仅站内信: UserId={UserId}, Title={Title}",
                        userId, title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "外部通道外发失败: UserId={UserId}", userId);
            }
        }

        /// <summary>通道网关是否就绪（配置键 {channel}:Enabled，默认 false=未接网关）</summary>
        private bool IsChannelReady(string channel)
            => _configuration.GetValue<bool?>($"{channel}:Enabled") ?? false;
    }
}