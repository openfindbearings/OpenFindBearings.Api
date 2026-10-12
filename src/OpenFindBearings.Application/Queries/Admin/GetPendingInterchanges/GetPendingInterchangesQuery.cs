using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingInterchanges
{
    /// <summary>平台待审核替代候选查询（v2.16.0）：审核队列</summary>
    public record GetPendingInterchangesQuery : IRequest<PagedResult<InterchangeAdminDto>>, IQuery
    {
        /// <summary>页码</summary>
        public int Page { get; init; } = 1;

        /// <summary>每页条数</summary>
        public int PageSize { get; init; } = 20;
    }

    /// <summary>平台待审核替代候选处理器</summary>
    public class GetPendingInterchangesQueryHandler : IRequestHandler<GetPendingInterchangesQuery, PagedResult<InterchangeAdminDto>>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetPendingInterchangesQueryHandler> _logger;

        public GetPendingInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetPendingInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        /// <summary>分页查询待审候选并映射管理行 DTO（补齐提出商户名称）</summary>
        public async Task<PagedResult<InterchangeAdminDto>> Handle(GetPendingInterchangesQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await _interchangeRepository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);

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