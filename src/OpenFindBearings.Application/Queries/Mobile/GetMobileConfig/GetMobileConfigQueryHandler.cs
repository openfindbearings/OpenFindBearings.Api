using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Mobile.GetMobileConfig
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

            // 改动说明：接入站点展示配置，移动端从 SystemConfigs 读取站点名称/备案号/客服联系方式
            var siteName = configs.FirstOrDefault(c => c.Key == "SiteName");
            if (siteName != null) result.SiteName = siteName.Value;

            var beiAn = configs.FirstOrDefault(c => c.Key == "Site.BeiAn");
            if (beiAn != null) result.SiteBeiAn = beiAn.Value;

            var customerService = configs.FirstOrDefault(c => c.Key == "Site.CustomerService");
            if (customerService != null) result.CustomerService = customerService.Value;

            return result;
        }
    }
}
