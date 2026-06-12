using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Brands.HardDeleteBrand
{
    /// <summary>
    /// 彻底删除品牌命令（物理删除，仅限已软删除的记录）
    /// </summary>
    public record HardDeleteBrandCommand(Guid Id) : IRequest, ICommand;
}
