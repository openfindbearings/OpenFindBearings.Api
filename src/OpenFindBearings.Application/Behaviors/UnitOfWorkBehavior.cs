using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;

namespace OpenFindBearings.Application.Behaviors
{
    /// <summary>
    /// 工作单元行为 - 自动管理数据库事务和提交
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    public class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse>> _logger;

        public UnitOfWorkBehavior(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // 查询请求不触发工作单元
            if (request is IQuery)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;
            _logger.LogDebug("开始处理命令: {RequestName}", requestName);

            try
            {
                // 执行实际的命令处理
                var response = await next();

                // 获取领域事件（在保存之前）
                var domainEvents = _unitOfWork.GetDomainEvents();

                // 保存更改到数据库
                var savedCount = await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 提交成功后，发布领域事件
                foreach (var domainEvent in domainEvents)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }

                // 清空已发布的事件
                _unitOfWork.ClearDomainEvents();

                _logger.LogDebug(
                    "命令处理完成: {RequestName}, 保存了 {SavedCount} 条更改, 发布了 {EventCount} 个事件",
                    requestName, savedCount, domainEvents.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "命令处理失败: {RequestName}", requestName);
                throw;
            }
        }
    }

    /// <summary>
    /// 标记接口：表示只读查询（不会触发 UnitOfWorkBehavior 的事务）
    /// </summary>
    public interface IQuery { }

    /// <summary>
    /// 标记接口：表示写操作（会触发 UnitOfWorkBehavior 的事务）
    /// </summary>
    public interface ICommand { }
}
