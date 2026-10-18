using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Queries.Admin.GetInterchanges
{
    /// <summary>平台替代关系管理列表查询（v2.17.0 字典化）：可按关键字、创建时间与来源过滤、分页</summary>
    public record GetInterchangesQuery : IRequest<PagedResult<InterchangeAdminDto>>, IQuery
    {
        /// <summary>关键字（源/目标型号模糊）</summary>
        public string? Keyword { get; init; }

        /// <summary>
        /// 创建起始日期（业务日历 yyyy-MM-dd，含）——按 BusinessClock 日界换算为 UTC 过滤
        /// 改动说明（v1.50.0 下钻筛选）
        /// </summary>
        public DateOnly? CreatedFrom { get; init; }

        /// <summary>创建截止日期（业务日历 yyyy-MM-dd，含）</summary>
        public DateOnly? CreatedTo { get; init; }

        /// <summary>来源筛选：true=仅互联网来源 / false=仅非互联网来源 / null=不限</summary>
        public bool? InternetSource { get; init; }

        /// <summary>页码</summary>
        public int Page { get; init; } = 1;

        /// <summary>每页条数</summary>
        public int PageSize { get; init; } = 20;
    }

    /// <summary>平台替代关系管理列表处理器</summary>
    public class GetInterchangesQueryHandler : IRequestHandler<GetInterchangesQuery, PagedResult<InterchangeAdminDto>>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<GetInterchangesQueryHandler> _logger;

        // 改动说明（v2.17.0 字典化）：去掉商户仓储依赖（不再有"提出候选商户名称"需补齐）
        public GetInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<GetInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>分页查询并映射管理行 DTO</summary>
        public async Task<PagedResult<InterchangeAdminDto>> Handle(GetInterchangesQuery request, CancellationToken cancellationToken)
        {
            // 改动说明（v1.50.0 下钻筛选）：创建时间按业务日历（BusinessClock）换算为 UTC 区间，
            //   下限取当日零点（含）、上限取"次日零点（不含）"以实现含当日
            DateTime? createdFromUtc = request.CreatedFrom.HasValue
                ? DateTime.SpecifyKind(request.CreatedFrom.Value.ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                : null;
            DateTime? createdToUtc = request.CreatedTo.HasValue
                ? DateTime.SpecifyKind(request.CreatedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                : null;

            var (items, total) = await _interchangeRepository.GetPagedAsync(
                request.Keyword, createdFromUtc, createdToUtc, request.InternetSource,
                request.Page, request.PageSize, cancellationToken);

            var result = items.Select(bi => bi.ToAdminDto()).ToList();

            return new PagedResult<InterchangeAdminDto>
            {
                Items = result,
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}