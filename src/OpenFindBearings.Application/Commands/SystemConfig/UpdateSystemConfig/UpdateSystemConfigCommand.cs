using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.SystemConfig.UpdateSystemConfig
{
    /// <summary>
    /// 更新系统配置命令
    /// </summary>
    public record UpdateSystemConfigCommand : IRequest, ICommand
    {
        /// <summary>
        /// 配置键
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        public string Value { get; init; } = string.Empty;

        /// <summary>
        /// 更新人ID
        /// </summary>
        public Guid UpdatedBy { get; init; }
    }
}
