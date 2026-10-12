using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantInterchanges.GetMyInterchanges
{
    /// <summary>商家查看本人提交的替代候选查询（v2.16.0，含各审核状态与驳回理由）</summary>
    public record GetMyInterchangesQuery : IRequest<List<InterchangeAdminDto>>, IQuery
    {
        /// <summary>商户ID（端点从当前商户上下文注入）</summary>
        public Guid MerchantId { get; init; }
    }

    /// <summary>商家查看本人替代候选处理器</summary>
    public class GetMyInterchangesQueryHandler : IRequestHandler<GetMyInterchangesQuery, List<InterchangeAdminDto>>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<GetMyInterchangesQueryHandler> _logger;

        public GetMyInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<GetMyInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>按提出商户查询并映射</summary>
        public async Task<List<InterchangeAdminDto>> Handle(GetMyInterchangesQuery request, CancellationToken cancellationToken)
        {
            var items = await _interchangeRepository.GetByProposerAsync(request.MerchantId, cancellationToken);
            return items.Select(bi => bi.ToAdminDto()).ToList();
        }
    }
}