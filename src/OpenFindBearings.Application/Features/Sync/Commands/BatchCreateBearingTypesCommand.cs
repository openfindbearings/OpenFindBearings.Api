using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    /// <summary>
    /// 批量创建轴承类型命令
    /// </summary>
    public record BatchCreateBearingTypesCommand : IRequest<BatchResult>
    {
        public List<SyncBearingTypeDto> BearingTypes { get; set; } = new();
        public SyncMode Mode { get; set; } = SyncMode.Upsert;
    }
}
