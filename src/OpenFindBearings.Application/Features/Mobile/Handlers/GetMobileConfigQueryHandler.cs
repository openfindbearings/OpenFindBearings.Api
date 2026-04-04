using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Mobile.DTOs;
using OpenFindBearings.Application.Features.Mobile.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Mobile.Handlers
{
    /// <summary>
    /// 获取移动端配置查询处理器
    /// </summary>
    public class GetMobileConfigQueryHandler : IRequestHandler<GetMobileConfigQuery, MobileConfigDto>
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<GetMobileConfigQueryHandler> _logger;

        public GetMobileConfigQueryHandler(
            ISystemConfigRepository systemConfigRepository,
            ILogger<GetMobileConfigQueryHandler> logger)
        {
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task<MobileConfigDto> Handle(
            GetMobileConfigQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取移动端配置");

            var configs = await _systemConfigRepository.GetAllAsync(cancellationToken);

            var result = new MobileConfigDto();

            // 读取移动端配置
            var appVersion = configs.FirstOrDefault(c => c.Key == "Mobile.AppVersion");
            if (appVersion != null) result.AppVersion = appVersion.Value;

            var minVersion = configs.FirstOrDefault(c => c.Key == "Mobile.MinVersion");
            if (minVersion != null) result.MinVersion = minVersion.Value;

            var forceUpdate = configs.FirstOrDefault(c => c.Key == "Mobile.ForceUpdate");
            if (forceUpdate != null && bool.TryParse(forceUpdate.Value, out var force))
                result.ForceUpdate = force;

            var downloadUrl = configs.FirstOrDefault(c => c.Key == "Mobile.DownloadUrl");
            if (downloadUrl != null) result.DownloadUrl = downloadUrl.Value;

            // API 端点配置
            result.Endpoints = new Dictionary<string, string>
            {
                ["search"] = "/api/mobile/bearings/light",
                ["detail"] = "/api/bearings/{id}",
                ["login"] = "/connect/token",
                ["register"] = "/api/account/register",
                ["home"] = "/api/mobile/home",
                ["favorites"] = "/api/user/favorites/bearings",
                ["profile"] = "/api/user/me"
            };

            // 其他设置
            result.Settings = new Dictionary<string, object>
            {
                ["pageSize"] = 10,
                ["maxPageSize"] = 50,
                ["enableCache"] = true,
                ["cacheExpiry"] = 300,
                ["imageQuality"] = 80,
                ["maxUploadSize"] = 5242880 // 5MB
            };

            return result;
        }
    }
}
