using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    /// <summary>
    /// 批量创建品牌命令
    /// </summary>
    public record BatchCreateBrandsCommand : IRequest<BatchResult>
    {
        public List<SyncBrandDto> Brands { get; set; } = new();
        public SyncMode Mode { get; set; } = SyncMode.Upsert;
    }
}
