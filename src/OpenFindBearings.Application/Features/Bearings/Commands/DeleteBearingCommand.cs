using MediatR;

namespace OpenFindBearings.Application.Features.Bearings.Commands
{
    /// <summary>
    /// 删除轴承命令
    /// </summary>
    public record DeleteBearingCommand(Guid Id) : IRequest;
}
