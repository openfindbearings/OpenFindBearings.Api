using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IBearingRepository
    {
        Task<Bearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Bearing?> GetByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<Bearing>> SearchAsync(BearingSearchParams searchParams, CancellationToken cancellationToken = default);
        Task AddAsync(Bearing bearing, CancellationToken cancellationToken = default);
        Task UpdateAsync(Bearing bearing, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string partNumber, CancellationToken cancellationToken = default);

        // 新增获取热门产品的方法
        Task<IEnumerable<Bearing>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default);
    }
}
