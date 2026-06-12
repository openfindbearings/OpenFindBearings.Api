using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Brands.RestoreBrand
{
    /// <summary>
    /// 恢复已删除品牌命令
    /// </summary>
    public record RestoreBrandCommand(Guid Id) : IRequest, ICommand;
}
