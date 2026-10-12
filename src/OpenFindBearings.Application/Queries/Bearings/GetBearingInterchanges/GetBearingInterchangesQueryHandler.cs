using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingInterchanges
{
    /// <summary>
    /// 获取轴承替代品处理器（v2.16.0）：仅返回已生效（Approved）且公域可见的替代关系，
    /// 并按"源∪双向反向"合并——双向标记因此真正生效。附带三级类型标签与责任提示字段
    /// </summary>
    public class GetBearingInterchangesQueryHandler : IRequestHandler<GetBearingInterchangesQuery, List<BearingInterchangeDto>>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<GetBearingInterchangesQueryHandler> _logger;

        public GetBearingInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<GetBearingInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        public async Task<List<BearingInterchangeDto>> Handle(GetBearingInterchangesQuery request, CancellationToken cancellationToken)
        {
            var interchanges = await _interchangeRepository.GetApprovedForBearingAsync(request.BearingId, cancellationToken);
            return interchanges.Select(bi => bi.ToInterchangeDto(request.BearingId)).ToList();
        }
    }
}