using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.BearingTypes.CreateBearingType
{
    /// <summary>
    /// 创建轴承类型命令
    /// </summary>
    public record CreateBearingTypeCommand : IRequest<Guid>, ICommand
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
