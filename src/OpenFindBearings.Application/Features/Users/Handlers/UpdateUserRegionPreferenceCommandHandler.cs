using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 更新用户地区偏好命令处理器
    /// </summary>
    public class UpdateUserRegionPreferenceCommandHandler : IRequestHandler<UpdateUserRegionPreferenceCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserRegionPreferenceCommandHandler> _logger;

        public UpdateUserRegionPreferenceCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateUserRegionPreferenceCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// 处理更新用户地区偏好
        /// </summary>
        public async Task Handle(UpdateUserRegionPreferenceCommand request, CancellationToken cancellationToken)
        {
            // 如果没有省份和城市信息，直接返回
            if (string.IsNullOrEmpty(request.Province) && string.IsNullOrEmpty(request.City))
                return;

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null) return;

            // 记录到日志（实际项目中可以存到 UserPreference 表）
            _logger.LogInformation("用户地区偏好: UserId={UserId}, Province={Province}, City={City}",
                request.UserId, request.Province, request.City);
        }
    }
}
