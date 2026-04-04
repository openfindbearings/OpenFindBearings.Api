using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    /// <summary>
    /// 批量创建轴承命令
    /// </summary>
    public record BatchCreateBearingsCommand : IRequest<BatchResult>
    {
        /// <summary>
        /// 轴承列表
        /// </summary>
        public List<SyncBearingDto> Bearings { get; set; } = new();

        /// <summary>
        /// 同步模式
        /// </summary>
        public SyncMode Mode { get; set; } = SyncMode.Upsert;
    }
}
