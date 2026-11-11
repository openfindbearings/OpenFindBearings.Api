using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.BearingTypes.UpdateBearingType
{
    public record UpdateBearingTypeCommand : IRequest, ICommand
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }

        // 改动说明（类型资料完善）：补充可在 Admin 维护的资料字段，全部可空
        public string? EnglishName { get; init; }
        public string? StructureFeatures { get; init; }
        public string? LoadCharacteristics { get; init; }
        public string? TypicalPrefixes { get; init; }
        public string? MainApplications { get; init; }
        public bool? IsSeparable { get; init; }
        public string? ImageUrl { get; init; }
    }
}
