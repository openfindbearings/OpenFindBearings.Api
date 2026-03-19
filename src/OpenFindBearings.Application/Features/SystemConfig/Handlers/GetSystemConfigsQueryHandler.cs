using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Application.Features.SystemConfig.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.SystemConfig.Handlers
{
    /// <summary>
    /// 获取系统配置列表查询处理器
    /// </summary>
    public class GetSystemConfigsQueryHandler : IRequestHandler<GetSystemConfigsQuery, List<SystemConfigDto>>
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetSystemConfigsQueryHandler> _logger;

        public GetSystemConfigsQueryHandler(
            ISystemConfigRepository systemConfigRepository,
            IUserRepository userRepository,
            ILogger<GetSystemConfigsQueryHandler> logger)
        {
            _systemConfigRepository = systemConfigRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<SystemConfigDto>> Handle(
            GetSystemConfigsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取系统配置列表: Group={Group}", request.Group);

            var configs = await _systemConfigRepository.GetAllAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Group))
            {
                configs = configs.Where(c => c.Group == request.Group).ToList();
            }

            var result = new List<SystemConfigDto>();
            foreach (var config in configs)
            {
                string updatedByName = "系统";
                if (config.UpdatedBy.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(config.UpdatedBy.Value, cancellationToken);
                    updatedByName = user?.Nickname ?? "未知用户";
                }

                result.Add(new SystemConfigDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value,
                    Description = config.Description,
                    Group = config.Group,
                    UpdatedAt = config.UpdatedAt ?? config.CreatedAt,
                    UpdatedBy = updatedByName
                });
            }

            return result;
        }
    }
}
