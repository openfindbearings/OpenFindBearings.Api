using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Queries.Admin.GetInterchanges
{
    /// <summary>平台替代关系管理列表查询（v2.16.0）：可按关键字与审核状态过滤、分页</summary>
    public record GetInterchangesQuery : IRequest<PagedResult<InterchangeAdminDto>>, IQuery
    {
        /// <summary>关键字（源/目标型号模糊）</summary>
        public string? Keyword { get; init; }

        /// <summary>审核状态过滤（0=已生效 1=待审核 2=已驳回；null=全部）</summary>
        public int? ReviewStatus { get; init; }

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
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetInterchangesQueryHandler> _logger;

        public GetInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        /// <summary>分页查询并映射管理行 DTO（补齐提出商户名称）</summary>
        public async Task<PagedResult<InterchangeAdminDto>> Handle(GetInterchangesQuery request, CancellationToken cancellationToken)
        {
            InterchangeReviewStatus? status = request.ReviewStatus.HasValue
                ? (InterchangeReviewStatus)request.ReviewStatus.Value
                : null;

            // 改动说明（v1.50.0 下钻筛选）：创建时间按业务日历（BusinessClock）换算为 UTC 区间，
            //   下限取当日零点（含）、上限取"次日零点（不含）"以实现含当日
            DateTime? createdFromUtc = request.CreatedFrom.HasValue
                ? DateTime.SpecifyKind(request.CreatedFrom.Value.ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                : null;
            DateTime? createdToUtc = request.CreatedTo.HasValue
                ? DateTime.SpecifyKind(request.CreatedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                : null;

            var (items, total) = await _interchangeRepository.GetPagedAsync(
                request.Keyword, status, createdFromUtc, createdToUtc, request.InternetSource,
                request.Page, request.PageSize, cancellationToken);

            var result = new List<InterchangeAdminDto>();
            foreach (var bi in items)
            {
                var dto = bi.ToAdminDto();
                if (dto.ProposedByMerchantId.HasValue)
                {
                    var merchant = await _merchantRepository.GetByIdAsync(dto.ProposedByMerchantId.Value, cancellationToken);
                    dto.ProposedByMerchantName = merchant?.Name;
                }
                result.Add(dto);
            }

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