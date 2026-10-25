using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Extensions;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 型号线索仓储实现
    /// 写入走原生 SQL 原子 upsert，查询按"型号库是否存在同型号"回填计算态
    /// </summary>
    public class BearingLeadRepository : IBearingLeadRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BearingLeadRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 记录一次型号线索出现
        /// 改动说明（v2.21.0 型号线索）：用 ON CONFLICT 原子累加，避免并发发布时唯一键冲突导致业务单失败；
        /// 不经过 EF 变更跟踪，也不调用 SaveChanges，因此不会污染业务事务
        /// </summary>
        public async Task RecordAsync(string partNumber, string? brand, BearingLeadSource source, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                return;

            var now = DateTime.UtcNow;
            var id = Guid.NewGuid();
            var trimmed = partNumber.Trim();
            var key = BearingLead.NormalizeKey(trimmed);
            var brandValue = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
            var sourceValue = (int)source;

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO ""BearingLeads""
    (""Id"", ""PartNumber"", ""PartNumberKey"", ""Brand"", ""Source"", ""OccurrenceCount"", ""FirstSeenAt"", ""LastSeenAt"", ""IsIgnored"", ""IsActive"", ""CreatedAt"")
VALUES
    ({id}, {trimmed}, {key}, {brandValue}, {sourceValue}, 1, {now}, {now}, false, true, {now})
ON CONFLICT (""PartNumberKey"") DO UPDATE SET
    ""OccurrenceCount"" = ""BearingLeads"".""OccurrenceCount"" + 1,
    ""LastSeenAt"" = {now},
    ""Source"" = {sourceValue},
    ""Brand"" = COALESCE(""BearingLeads"".""Brand"", {brandValue})",
                cancellationToken);
        }

        /// <summary>
        /// 判断型号库是否已收录该型号（忽略大小写，含已软删除型号）
        /// </summary>
        public async Task<bool> ExistsInLibraryAsync(string partNumberKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partNumberKey))
                return false;

            return await _context.Bearings.IgnoreQueryFilters()
                .AnyAsync(b => b.PartNumber.ToLower() == partNumberKey, cancellationToken);
        }

        /// <summary>
        /// 分页查询型号线索
        /// 改动说明（v2.21.0 型号线索）：状态是计算态——已忽略优先，其次看型号库是否已有同型号；
        /// 页码结果集再统一回填 IsResolved，避免逐行查询造成 N+1
        /// </summary>
        public async Task<PagedResult<BearingLead>> SearchAsync(BearingLeadSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var page = searchParams.Page < 1 ? 1 : searchParams.Page;
            var pageSize = searchParams.PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => searchParams.PageSize
            };

            var query = _context.BearingLeads.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
            {
                var keyword = searchParams.Keyword.Trim();
                query = query.Where(l => l.PartNumber.Contains(keyword));
            }

            var bearingKeys = _context.Bearings.IgnoreQueryFilters().Select(b => b.PartNumber.ToLower());

            query = searchParams.Status switch
            {
                BearingLeadStatus.Ignored => query.Where(l => l.IsIgnored),
                BearingLeadStatus.Resolved => query.Where(l => !l.IsIgnored && bearingKeys.Contains(l.PartNumberKey)),
                BearingLeadStatus.Pending => query.Where(l => !l.IsIgnored && !bearingKeys.Contains(l.PartNumberKey)),
                _ => query
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(l => l.OccurrenceCount)
                .ThenByDescending(l => l.LastSeenAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            if (items.Count > 0)
            {
                var keys = items.Select(i => i.PartNumberKey).Distinct().ToList();
                var existingKeys = await _context.Bearings.IgnoreQueryFilters()
                    .Where(b => keys.Contains(b.PartNumber.ToLower()))
                    .Select(b => b.PartNumber.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var resolvedSet = existingKeys.ToHashSet();
                foreach (var item in items)
                    item.SetResolved(resolvedSet.Contains(item.PartNumberKey));
            }

            return new PagedResult<BearingLead>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// 按 ID 获取型号线索
        /// </summary>
        public async Task<BearingLead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.BearingLeads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        /// <summary>
        /// 更新型号线索（标记为已修改，提交由 UnitOfWork 负责）
        /// </summary>
        public Task UpdateAsync(BearingLead lead, CancellationToken cancellationToken = default)
        {
            _context.MarkModified(lead);
            return Task.CompletedTask;
        }
    }
}
