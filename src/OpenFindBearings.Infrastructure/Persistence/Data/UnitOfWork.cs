using MediatR;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public IReadOnlyList<INotification> GetDomainEvents()
        {
            // 获取所有有领域事件的实体
            var entities = _context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            // 收集所有领域事件
            var domainEvents = entities
                .SelectMany(x => x.DomainEvents)
                .ToList();

            return domainEvents;
        }

        public void ClearDomainEvents()
        {
            var entities = _context.ChangeTracker
                .Entries<BaseEntity>()
                .Select(x => x.Entity)
                .ToList();

            foreach (var entity in entities)
            {
                entity.ClearDomainEvents();
            }
        }
    }
}