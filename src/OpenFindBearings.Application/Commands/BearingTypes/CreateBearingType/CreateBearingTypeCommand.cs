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

        // 改动说明（类型资料完善）：新建时可直接录入资料字段，全部可空
        public string? EnglishName { get; init; }
        public string? StructureFeatures { get; init; }
        public string? LoadCharacteristics { get; init; }
        public string? TypicalPrefixes { get; init; }
        public string? MainApplications { get; init; }
        public bool? IsSeparable { get; init; }
        public string? ImageUrl { get; init; }
    }
}
