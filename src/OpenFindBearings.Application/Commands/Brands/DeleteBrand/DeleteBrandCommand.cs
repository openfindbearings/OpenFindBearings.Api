using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Brands.DeleteBrand
{
    /// <summary>
    /// 删除品牌命令（软删除）
    /// </summary>
    public record DeleteBrandCommand(Guid Id) : IRequest, ICommand;
}
