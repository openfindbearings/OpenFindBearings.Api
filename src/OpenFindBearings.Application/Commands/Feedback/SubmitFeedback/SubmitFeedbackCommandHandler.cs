using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.Feedback.SubmitFeedback
{
    /// <summary>
    /// 提交意见反馈命令处理器
    /// 校验分类与内容长度、图片前缀与张数，按业务日界检查每日提交上限后落库
    /// </summary>
    public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Guid>
    {
        private readonly IUserFeedbackRepository _feedbacks;
        private readonly ISystemConfigRepository _configs;
        private readonly ILogger<SubmitFeedbackCommandHandler> _logger;

        /// <summary>
        /// 构造：注入反馈仓储、系统配置与日志
        /// </summary>
        public SubmitFeedbackCommandHandler(
            IUserFeedbackRepository feedbacks,
            ISystemConfigRepository configs,
            ILogger<SubmitFeedbackCommandHandler> logger)
        {
            _feedbacks = feedbacks;
            _configs = configs;
            _logger = logger;
        }

        /// <summary>
        /// 执行提交：分类合法性 → 内容长度 → 图片校验 → 每日限额 → 落库
        /// </summary>
        public async Task<Guid> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
                throw new InvalidOperationException("请先登录后再提交反馈");

            if (!Enum.IsDefined(typeof(FeedbackCategory), request.Category))
                throw new InvalidOperationException("反馈分类不正确");

            var content = (request.Content ?? string.Empty).Trim();
            if (content.Length < UserFeedback.MinContentLength)
                throw new InvalidOperationException($"反馈内容至少 {UserFeedback.MinContentLength} 个字");
            if (content.Length > UserFeedback.MaxContentLength)
                throw new InvalidOperationException($"反馈内容不能超过 {UserFeedback.MaxContentLength} 个字");

            var images = (request.ImageUrls ?? new List<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .ToList();
            if (images.Count > UserFeedback.MaxImages)
                throw new InvalidOperationException($"最多上传 {UserFeedback.MaxImages} 张图片");
            // 图片必须是本系统上传端点产出的相对路径，防止用户塞入任意外链
            if (images.Any(u => !u.StartsWith(UserFeedback.ImagePathPrefix, StringComparison.Ordinal)))
                throw new InvalidOperationException("图片地址不合法，请重新上传");

            if (!string.IsNullOrWhiteSpace(request.Contact)
                && request.Contact.Trim().Length > UserFeedback.MaxContactLength)
                throw new InvalidOperationException($"联系方式不能超过 {UserFeedback.MaxContactLength} 个字");

            // 每日上限走系统配置，0 表示不限制；按业务日界统计（与签到/寻货额度同口径）
            var dailyLimit = await _configs.GetValueAsync<int?>("Business.FeedbackDailyLimit", null) ?? 5;
            if (dailyLimit > 0)
            {
                var submittedToday = await _feedbacks.CountSinceAsync(request.UserId, BusinessClock.TodayUtc, cancellationToken);
                if (submittedToday >= dailyLimit)
                    throw new InvalidOperationException($"今天已提交 {submittedToday} 条反馈，请明天再试");
            }

            var feedback = new UserFeedback(
                request.UserId,
                request.Category,
                content,
                images,
                request.Contact,
                request.ClientInfo);

            await _feedbacks.AddAsync(feedback, cancellationToken);

            _logger.LogInformation("意见反馈已提交: FeedbackId={FeedbackId}, User={UserId}, Category={Category}, Images={Images}",
                feedback.Id, request.UserId, request.Category, images.Count);

            return feedback.Id;
        }
    }
}