using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingInterchanges
{
    public class GetBearingInterchangesQueryHandler : IRequestHandler<GetBearingInterchangesQuery, List<BearingDto>>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingInterchangesQueryHandler> _logger;

        public GetBearingInterchangesQueryHandler(
            IBearingInterchangeRepository interchangeRepository,
            IBearingRepository bearingRepository,
            ILogger<GetBearingInterchangesQueryHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<List<BearingDto>> Handle(GetBearingInterchangesQuery request, CancellationToken cancellationToken)
        {
            var interchanges = await _interchangeRepository.GetBySourceBearingAsync(request.BearingId, cancellationToken);

            var result = new List<BearingDto>();

            foreach (var interchange in interchanges)
            {
                var bearing = await _bearingRepository.GetByIdAsync(interchange.TargetBearingId, cancellationToken);
                if (bearing != null)
                {
                    result.Add(bearing.ToDto());
                }
            }

            return result;
        }
    }
}
