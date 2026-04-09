using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateBearingTypes
{
    /// <summary>
    /// 批量创建轴承类型命令
    /// </summary>
    public record BatchCreateBearingTypesCommand : IRequest<BatchResult>, ICommand
    {
        public List<SyncBearingTypeDto> BearingTypes { get; set; } = new();
        public SyncMode Mode { get; set; } = SyncMode.Upsert;
    }
}
