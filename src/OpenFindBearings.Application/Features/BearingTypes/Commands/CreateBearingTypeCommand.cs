using MediatR;

namespace OpenFindBearings.Application.Features.BearingTypes.Commands
{
    /// <summary>
    /// 创建轴承类型命令
    /// </summary>
    public record CreateBearingTypeCommand : IRequest<Guid>
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
