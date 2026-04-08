using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateBearings
{
    /// <summary>
    /// 批量创建轴承命令
    /// </summary>
    public record BatchCreateBearingsCommand : IRequest<BatchResult>, ICommand
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
