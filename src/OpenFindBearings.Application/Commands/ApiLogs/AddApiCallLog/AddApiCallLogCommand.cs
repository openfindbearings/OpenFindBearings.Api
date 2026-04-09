using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Commands.ApiLogs.AddApiCallLog
{
    public record AddApiCallLogCommand : IRequest, ICommand
    {
        public ApiCallLog Log { get; init; } = null!;
    }
}
