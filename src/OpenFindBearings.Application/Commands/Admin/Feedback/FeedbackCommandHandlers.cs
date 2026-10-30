using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Admin.Feedback
{
    /// <summary>
    /// 回复意见反馈命令（v2.22.1）
    /// 一次性回复：写入回复内容并把状态置为已处理，同时向提交人发站内信
    /// </summary>
    /// <param name="FeedbackId">反馈 ID</param>
    /// <param name="AdminUserId">回复的管理员用户 ID</param>
    /// <param name="Reply">回复内容</param>
    public record ReplyFeedbackCommand(Guid FeedbackId, Guid AdminUserId, string Reply) : IRequest, ICommand;

    /// <summary>
    /// 变更意见反馈处理状态命令
    /// 仅做分诊流转（待处理/处理中/已处理/已关闭），不触发通知
    /// </summary>
    /// <param name="FeedbackId">反馈 ID</param>
    /// <param name="Status">目标状态</param>
    public record UpdateFeedbackStatusCommand(Guid FeedbackId, FeedbackStatus Status) : IRequest, ICommand;

    /// <summary>
    /// 意见反馈管理命令处理器
    /// 负责回复落库（原因校验）与状态流转，两个命令共用同一仓储
    /// </summary>
    public class FeedbackCommandHandlers :
        IRequestHandler<ReplyFeedbackCommand>,
        IRequestHandler<UpdateFeedbackStatusCommand>
    {
        private readonly IUserFeedbackRepository _feedbacks;
        private readonly ILogger<FeedbackCommandHandlers> _logger;

        /// <summary>
        /// 构造：注入反馈仓储与日志
        /// </summary>
        public FeedbackCommandHandlers(
            IUserFeedbackRepository feedbacks,
            ILogger<FeedbackCommandHandlers> logger)
        {
            _feedbacks = feedbacks;
            _logger = logger;
        }

        /// <summary>
        /// 回复反馈：校验存在性与内容长度后写入回复并置为已处理（实体抛事件通知提交人）
        /// </summary>
        public async Task Handle(ReplyFeedbackCommand request, CancellationToken cancellationToken)
        {
            var feedback = await _feedbacks.GetByIdAsync(request.FeedbackId, cancellationToken);
            if (feedback == null)
                throw new InvalidOperationException("反馈不存在或已被删除");

            if (string.IsNullOrWhiteSpace(request.Reply))
                throw new InvalidOperationException("请填写回复内容");

            feedback.Reply(request.AdminUserId, request.Reply);
            await _feedbacks.UpdateAsync(feedback, cancellationToken);

            _logger.LogInformation("意见反馈已回复: FeedbackId={FeedbackId}, Admin={AdminUserId}",
                request.FeedbackId, request.AdminUserId);
        }

        /// <summary>
        /// 变更处理状态：实体侧禁止把已回复反馈退回待处理
        /// </summary>
        public async Task Handle(UpdateFeedbackStatusCommand request, CancellationToken cancellationToken)
        {
            var feedback = await _feedbacks.GetByIdAsync(request.FeedbackId, cancellationToken);
            if (feedback == null)
                throw new InvalidOperationException("反馈不存在或已被删除");

            feedback.SetStatus(request.Status);
            await _feedbacks.UpdateAsync(feedback, cancellationToken);

            _logger.LogInformation("意见反馈状态变更: FeedbackId={FeedbackId}, Status={Status}",
                request.FeedbackId, request.Status);
        }
    }
}