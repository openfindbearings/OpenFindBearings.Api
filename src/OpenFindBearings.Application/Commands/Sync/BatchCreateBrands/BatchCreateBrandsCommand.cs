using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Sync.Commands
{
    /// <summary>
    /// 批量创建品牌命令
    /// </summary>
    public record BatchCreateBrandsCommand : IRequest<BatchResult>, ICommand
    {
        public List<SyncBrandDto> Brands { get; set; } = new();
        public SyncMode Mode { get; set; } = SyncMode.Upsert;
    }
}
