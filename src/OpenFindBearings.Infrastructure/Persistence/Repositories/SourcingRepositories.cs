using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Extensions;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 寻货需求仓储实现（v1.35.0）
    /// </summary>
    public class SourcingDemandRepository : ISourcingDemandRepository
    {
        private readonly ApplicationDbContext _context;

        public SourcingDemandRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<SourcingDemand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _context.Set<SourcingDemand>().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(SourcingDemand demand, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>().AddAsync(demand, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(SourcingDemand demand, CancellationToken cancellationToken = default)
        {
            _context.MarkModified(demand);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<(List<SourcingDemand> Items, int Total)> GetListAsync(int? status, string? keyword, bool onlyOpen,
            int page, int pageSize, bool pinFirst = false, string? brand = null, string? region = null,
            bool newestFirst = true, bool publicOnly = false, bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            // 改动说明（v2.12.0 列表删除）：feed/商家列表排除软删单（软删仅发布方视角隐藏，数据保留）；
            // 改动说明（v1.8.2 Admin 治理可见性）：includeDeleted=true 时不排除，让管理员仍能审计已删单
            var query = _context.Set<SourcingDemand>().AsQueryable();
            if (!includeDeleted)
                query = query.Where(d => !d.IsDeleted);

            // 改动说明（v1.8.0 公开/邀请）：公开大厅/反向导购仅取公开单，邀请单只对收件人与发布人可见
            if (publicOnly)
                query = query.Where(d => d.Visibility == SourcingVisibility.Public);

if (status.HasValue)
                query = query.Where(d => d.Status == (SourcingDemandStatus)status.Value);
            if (onlyOpen)
                query = query.Where(d => d.Status == SourcingDemandStatus.Published && d.ExpiryAt > DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // Npgsql 可翻译要求 ToLower（ToLowerInvariant 运行时不可译，既有约束）
                var kw = keyword.Trim().ToLower();
                query = query.Where(d => d.PartNumber.ToLower().Contains(kw));
            }
            // 改动说明（v1.4.0 大厅筛选）：品牌/收货地区均为发布时自由文本，按包含匹配（ToLower 翻译约束同上）
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var b = brand.Trim().ToLower();
                query = query.Where(d => d.Brand != null && d.Brand.ToLower().Contains(b));
            }
            if (!string.IsNullOrWhiteSpace(region))
            {
                var r = region.Trim().ToLower();
                query = query.Where(d => d.Region != null && d.Region.ToLower().Contains(r));
            }

            var total = await query.CountAsync(cancellationToken);
            // 改动说明（v2.10.0 寻货置顶）：公开大厅 pinFirst 时有效置顶排前——
            // 排序键取"未过期的 PinnedUntil，否则最小时间"，过期旧值不插队；
            // v1.4.0 新增 newestFirst：发布时间升降序切换（置顶仍恒排前，付费曝光不受排序影响）
            var ordered = pinFirst
                ? (newestFirst
                    ? query.OrderByDescending(d => d.PinnedUntil != null && d.PinnedUntil > DateTime.UtcNow
                        ? d.PinnedUntil : DateTime.MinValue)
                        .ThenByDescending(d => d.CreatedAt)
                    : query.OrderByDescending(d => d.PinnedUntil != null && d.PinnedUntil > DateTime.UtcNow
                        ? d.PinnedUntil : DateTime.MinValue)
                        .ThenBy(d => d.CreatedAt))
                : (newestFirst
                    ? query.OrderByDescending(d => d.CreatedAt)
                    : query.OrderBy(d => d.CreatedAt));
            var items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <inheritdoc/>
        public async Task<List<SourcingDemand>> GetByPublisherAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                // 改动说明（v2.12.0 商户名义发布）：排除商户名义单（PublisherMerchantId 非空）——
                // 个人"我的寻货"只看个人名义，商户单归商户工作台，双体系不混排
                .Where(d => d.PublisherUserId == userId && d.PublisherMerchantId == null && !d.IsDeleted)
                // 改动说明（v2.12.0 列表体验）：进行中单恒排前、其余按时间倒序——
                // 终态单不再沉没可用操作，与大厅"进行中即广场"口径一致
                .OrderBy(d => d.Status == SourcingDemandStatus.Published ? 0 : 1)
                .ThenByDescending(d => d.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingDemand>> GetByPublisherMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                .Where(d => d.PublisherMerchantId == merchantId && !d.IsDeleted)
                // 改动说明（v2.12.0）：商户"寻货发布"列表同样进行中恒排前（与个人口径一致）
                .OrderBy(d => d.Status == SourcingDemandStatus.Published ? 0 : 1)
.ThenByDescending(d => d.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingDemand>> GetOpenInvitedForMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                // 改动说明（v1.8.0 公开/邀请）：邀请给本商户的进行中需求（收件人快照 ANY 判定、未软删、未过期）
                .Where(d => d.Visibility == SourcingVisibility.Invited
                    && d.InvitedMerchantIds.Contains(merchantId) && !d.IsDeleted
                    && d.Status == SourcingDemandStatus.Published && d.ExpiryAt > DateTime.UtcNow)
                .OrderByDescending(d => d.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<int> CountPublishedTodayAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                .CountAsync(d => d.PublisherUserId == userId && d.CreatedAt >= BusinessClock.TodayUtc, cancellationToken);

        /// <inheritdoc/>
        public Task<SourcingDemand?> GetByPublisherAndKeyAsync(Guid publisherUserId, string idempotencyKey,
            CancellationToken cancellationToken = default)
            // 改动说明（v1.8.2 彻底幂等）：命中即"这是同一次提交的重放"，调用方直接返回原单（不建单、不扣分）
            => _context.Set<SourcingDemand>().FirstOrDefaultAsync(
                d => d.PublisherUserId == publisherUserId && d.IdempotencyKey == idempotencyKey, cancellationToken);

        /// <inheritdoc/>
        public async Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default)
        {
            // 惰性过期统一收口：进行中且已过 ExpiryAt → Expired（feed 顶部与详情读时各调一次，成本一条 UPDATE）
            return await _context.Set<SourcingDemand>()
.Where(d => d.Status == SourcingDemandStatus.Published && d.ExpiryAt <= DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, SourcingDemandStatus.Expired)
                    .SetProperty(d => d.ClosedAt, DateTime.UtcNow), cancellationToken);
        }
    }

    /// <summary>
    /// 寻货应答仓储实现（v1.35.0）
    /// </summary>
    public class SourcingResponseRepository : ISourcingResponseRepository
    {
        private readonly ApplicationDbContext _context;

        public SourcingResponseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<SourcingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _context.Set<SourcingResponse>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(SourcingResponse response, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>().AddAsync(response, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(SourcingResponse response, CancellationToken cancellationToken = default)
        {
            _context.MarkModified(response);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetByDemandAsync(Guid demandId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                // 改动说明（v1.8.2 撤销应答软取消）：撤销行不进比价展示（等价原删行对外语义）
                .Where(r => r.DemandId == demandId && r.Status != SourcingResponseStatus.Cancelled)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public Task<SourcingResponse?> GetByDemandAndMerchantAsync(Guid demandId, Guid merchantId, CancellationToken cancellationToken = default)
            => _context.Set<SourcingResponse>().FirstOrDefaultAsync(
                r => r.DemandId == demandId && r.MerchantId == merchantId, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                // 改动说明（v1.8.2 撤销应答软取消）：撤销行不出现在"我的应答"（撤销即从我方视角移除）
                .Where(r => r.MerchantId == merchantId && r.Status != SourcingResponseStatus.Cancelled)
                // 改动说明（v2.12.0）：待处理应答恒排前（还活着的商机优先），已选定/未选中按时间倒序
                .OrderBy(r => r.Status == SourcingResponseStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<int> CountRespondedTodayAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .CountAsync(r => r.MerchantId == merchantId && r.CreatedAt >= BusinessClock.TodayUtc, cancellationToken);

        /// <inheritdoc/>
        public async Task<int> CountRespondedTodayByUserAsync(Guid userId, CancellationToken cancellationToken = default)
            // 改动说明（v2.7.0 G2 三件套）：按操作人统计今日新增应答（业务日界起点口径与额度评估一致）
            => await _context.Set<SourcingResponse>()
                .CountAsync(r => r.RespondedUserId == userId && r.CreatedAt >= BusinessClock.TodayUtc, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetPendingByDemandAsync(Guid demandId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .Where(r => r.DemandId == demandId && r.Status == SourcingResponseStatus.Pending)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponseItem>> GetItemsAsync(Guid responseId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponseItem>()
                .Where(i => i.ResponseId == responseId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task DeleteItemsAsync(Guid responseId, CancellationToken cancellationToken = default)
        {
            // 改动说明（v1.5.0 多行标书）：重复应答=整体替换行，先清旧行再写新行
            var items = await _context.Set<SourcingResponseItem>()
                .Where(i => i.ResponseId == responseId)
                .ToListAsync(cancellationToken);
            _context.Set<SourcingResponseItem>().RemoveRange(items);
        }

        /// <inheritdoc/>
        public async Task AddItemsAsync(IEnumerable<SourcingResponseItem> items, CancellationToken cancellationToken = default)
            // 改动说明（v1.5.0 多行标书）：显式 Add 行，不走聚合根导航集合，
            // 规避 EF"导航发现新实体+更新根"的 Modified 误判陷阱
            => await _context.Set<SourcingResponseItem>().AddRangeAsync(items, cancellationToken);

        /// <inheritdoc/>
        public Task<int> GetCountedDealCountAsync(Guid merchantId, DateTime sinceUtc,
            CancellationToken cancellationToken = default)
            // 改动说明（v2.18.0 撮合成交链）：卡级"成功撮合"的唯一统计口——
            // 计分（IsCounted）且谈成（Confirmed），按回执时刻落在滚动窗口内
            => _context.Set<SourcingResponse>().CountAsync(r =>
                r.MerchantId == merchantId
                && r.IsCounted
                && r.FeedbackOutcome == SourcingDealOutcome.Confirmed
                && r.FeedbackAt >= sinceUtc, cancellationToken);

        /// <inheritdoc/>
        public Task<bool> HasCountedDealAsync(Guid merchantId, Guid publisherUserId, DateTime sinceUtc,
            Guid? excludeResponseId = null, CancellationToken cancellationToken = default)
            // 改动说明（v1.57.1 修 bug）：增加 excludeResponseId——回执重复提交（更新同一条）时，
            //   原逻辑会把"自己这条已计分"当成重复从而把 counted 翻成 false，误撤销一次成功撮合；
            //   去重只应拦截"另一条"窗口内计分成交
            => (from r in _context.Set<SourcingResponse>()
                join d in _context.Set<SourcingDemand>() on r.DemandId equals d.Id
                where r.MerchantId == merchantId
                      && d.PublisherUserId == publisherUserId
                      && r.IsCounted
                      && r.FeedbackOutcome == SourcingDealOutcome.Confirmed
                      && r.FeedbackAt >= sinceUtc
                      && (!excludeResponseId.HasValue || r.Id != excludeResponseId.Value)
                select r.Id).AnyAsync(cancellationToken);

        /// <inheritdoc/>
        public Task<int> CountFeedbackByIpSinceAsync(string ip, DateTime sinceUtc,
            CancellationToken cancellationToken = default)
            // 改动说明（v2.18.0 异常检测）：同 IP 在时间窗内的回执提交数（跨商户集中=可疑）
            => _context.Set<SourcingResponse>().CountAsync(r =>
                r.FeedbackIp == ip && r.FeedbackAt >= sinceUtc, cancellationToken);

        /// <inheritdoc/>
        public async Task<(List<SourcingResponse> Items, int Total)> GetFeedbackPagedAsync(Guid? merchantId,
            SourcingDealOutcome? outcome, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<SourcingResponse>().Where(r => r.FeedbackOutcome != null);
            if (merchantId.HasValue)
                query = query.Where(r => r.MerchantId == merchantId.Value);
            if (outcome.HasValue)
                query = query.Where(r => r.FeedbackOutcome == outcome.Value);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(r => r.FeedbackAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }
    }

    /// <summary>
    /// 撮合会话仓储实现（v2.18.0 T1 会话）：会话与消息读写；未读按已读水位实时推算，不落冗余计数
    /// </summary>
    public class SourcingConversationRepository : ISourcingConversationRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>构造：注入应用 DbContext</summary>
        public SourcingConversationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<SourcingConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _context.Set<SourcingConversation>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        /// <inheritdoc/>
        public Task<SourcingConversation?> GetByDemandAndMerchantAsync(Guid demandId, Guid merchantId,
            CancellationToken cancellationToken = default)
            => _context.Set<SourcingConversation>().FirstOrDefaultAsync(
                c => c.DemandId == demandId && c.MerchantId == merchantId, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(SourcingConversation conversation, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingConversation>().AddAsync(conversation, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(SourcingConversation conversation, CancellationToken cancellationToken = default)
        {
            // 仓储只负责标记 Modified，实际提交由 UnitOfWork（MediatR 管道）完成
            _context.Set<SourcingConversation>().Update(conversation);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task AddMessageAsync(SourcingMessage message, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingMessage>().AddAsync(message, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingMessage>> GetMessagesAsync(Guid conversationId, DateTime? before, int limit,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<SourcingMessage>().Where(m => m.ConversationId == conversationId);
            if (before.HasValue)
                query = query.Where(m => m.CreatedAt < before.Value);
            return await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<int> CountUnreadAsync(Guid conversationId, SourcingSenderSide side, DateTime? lastReadAt,
            CancellationToken cancellationToken = default)
            // 未读=对方发的消息中晚于本方已读水位者（水位为空=从未读，全部对方消息均未读）
            => _context.Set<SourcingMessage>().CountAsync(m =>
                m.ConversationId == conversationId
                && m.SenderSide != side
                && (!lastReadAt.HasValue || m.CreatedAt > lastReadAt.Value), cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingConversation>> GetForPublisherAsync(Guid publisherUserId, int skip, int take,
            CancellationToken cancellationToken = default)
            => await _context.Set<SourcingConversation>()
                .Where(c => c.PublisherUserId == publisherUserId)
                .OrderByDescending(c => c.LastMessageAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public Task<int> CountForPublisherAsync(Guid publisherUserId, CancellationToken cancellationToken = default)
            => _context.Set<SourcingConversation>().CountAsync(c => c.PublisherUserId == publisherUserId, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingConversation>> GetForMerchantAsync(List<Guid> userMerchantIds, int skip, int take,
            CancellationToken cancellationToken = default)
        {
            if (userMerchantIds.Count == 0)
                return new List<SourcingConversation>();
            return await _context.Set<SourcingConversation>()
                .Where(c => userMerchantIds.Contains(c.MerchantId))
                .OrderByDescending(c => c.LastMessageAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<int> CountForMerchantAsync(List<Guid> userMerchantIds, CancellationToken cancellationToken = default)
            => userMerchantIds.Count == 0
                ? Task.FromResult(0)
                : _context.Set<SourcingConversation>().CountAsync(c => userMerchantIds.Contains(c.MerchantId), cancellationToken);

        /// <inheritdoc/>
        public Task<int> CountUnreadMessagesForPublisherAsync(Guid publisherUserId,
            CancellationToken cancellationToken = default)
            // 改动说明（v1.57.1）：未读聚合下推到数据库，避免"只取前 200 个会话"造成的角标截断
            => (from c in _context.Set<SourcingConversation>()
                join m in _context.Set<SourcingMessage>() on c.Id equals m.ConversationId
                where c.PublisherUserId == publisherUserId
                      && m.SenderSide == SourcingSenderSide.Merchant
                      && (!c.PublisherLastReadAt.HasValue || m.CreatedAt > c.PublisherLastReadAt.Value)
                select m.Id).CountAsync(cancellationToken);

        /// <inheritdoc/>
        public Task<int> CountUnreadMessagesForMerchantAsync(List<Guid> userMerchantIds, Guid excludePublisherUserId,
            CancellationToken cancellationToken = default)
            // 改动说明（v1.57.1）：同上；本人以需求方身份参与的会话已计入需求方视图，此处排除避免重复
            => userMerchantIds.Count == 0
                ? Task.FromResult(0)
                : (from c in _context.Set<SourcingConversation>()
                   join m in _context.Set<SourcingMessage>() on c.Id equals m.ConversationId
                   where userMerchantIds.Contains(c.MerchantId)
                         && c.PublisherUserId != excludePublisherUserId
                         && m.SenderSide == SourcingSenderSide.Publisher
                         && (!c.MerchantLastReadAt.HasValue || m.CreatedAt > c.MerchantLastReadAt.Value)
                   select m.Id).CountAsync(cancellationToken);
    }
}
