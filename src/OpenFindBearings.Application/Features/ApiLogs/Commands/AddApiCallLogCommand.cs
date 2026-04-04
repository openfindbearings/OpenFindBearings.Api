using MediatR;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Features.ApiLogs.Commands
{
    public record AddApiCallLogCommand : IRequest
    {
        public ApiCallLog Log { get; init; } = null!;
    }
}
