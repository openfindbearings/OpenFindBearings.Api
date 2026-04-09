using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Abstractions;

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
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public UnitOfWorkBehavior(
            IServiceProvider serviceProvider,
            IMediator mediator,
            ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
        {
            _serviceProvider = serviceProvider;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is IQuery)
            {
                return await next();
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContext>();

            var requestName = typeof(TRequest).Name;
            _logger.LogDebug("UnitOfWork 开始处理 {RequestName}", requestName);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                var domainEvents = context.ChangeTracker
                    .Entries<BaseEntity>()
                    .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
                    .SelectMany(x => x.Entity.DomainEvents)
                    .ToList();

                foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
                {
                    entry.Entity.ClearDomainEvents();
                }

                var savedCount = await context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                foreach (var domainEvent in domainEvents)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }

                _logger.LogDebug("UnitOfWork 完成 {RequestName}，提交了 {SavedCount} 条更改，发布了 {EventCount} 个事件",
                    requestName, savedCount, domainEvents.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnitOfWork 处理 {RequestName} 失败，准备回滚事务", requestName);
                await transaction.RollbackAsync(cancellationToken);
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
