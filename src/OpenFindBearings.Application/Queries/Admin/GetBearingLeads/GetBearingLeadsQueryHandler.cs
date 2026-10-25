using MediatR;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetBearingLeads
{
    /// <summary>
    /// 获取型号线索列表查询处理器
    /// </summary>
    public class GetBearingLeadsQueryHandler : IRequestHandler<GetBearingLeadsQuery, PagedResult<BearingLeadDto>>
    {
        private readonly IBearingLeadRepository _bearingLeadRepository;

        /// <summary>
        /// 构造函数
        /// </summary>
        public GetBearingLeadsQueryHandler(IBearingLeadRepository bearingLeadRepository)
        {
            _bearingLeadRepository = bearingLeadRepository;
        }

        /// <summary>
        /// 执行查询：状态与来源枚举转中文展示名，其余字段原样透出
        /// </summary>
        public async Task<PagedResult<BearingLeadDto>> Handle(GetBearingLeadsQuery request, CancellationToken cancellationToken)
        {
            var result = await _bearingLeadRepository.SearchAsync(new BearingLeadSearchParams
            {
                Status = request.Status,
                Keyword = request.Keyword,
                Page = request.Page,
                PageSize = request.PageSize
            }, cancellationToken);

            var items = result.Items.Select(lead =>
            {
                // 改动说明（v2.21.0 型号线索）：状态是计算态——人工忽略优先，其次看型号库是否已有同型号
                var status = lead.IsIgnored
                    ? BearingLeadStatus.Ignored
                    : lead.IsResolved
                        ? BearingLeadStatus.Resolved
                        : BearingLeadStatus.Pending;

                return new BearingLeadDto
                {
                    Id = lead.Id,
                    PartNumber = lead.PartNumber,
                    Brand = lead.Brand,
                    OccurrenceCount = lead.OccurrenceCount,
                    Source = lead.Source.ToString(),
                    SourceText = lead.Source == BearingLeadSource.Demand ? "发布寻货" : "商家应答",
                    FirstSeenAt = lead.FirstSeenAt,
                    LastSeenAt = lead.LastSeenAt,
                    Status = status.ToString(),
                    StatusText = status switch
                    {
                        BearingLeadStatus.Ignored => "已忽略",
                        BearingLeadStatus.Resolved => "已入库",
                        _ => "待建档"
                    }
                };
            }).ToList();

            return new PagedResult<BearingLeadDto>(items, result.TotalCount, result.Page, result.PageSize);
        }
    }
}
