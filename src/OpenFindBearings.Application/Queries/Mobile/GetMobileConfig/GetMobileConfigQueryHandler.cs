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

            var forceUpdate = configs.FirstOrDefault(c => c.Key == "Mobile.ForceUpdate");
            if (forceUpdate != null && bool.TryParse(forceUpdate.Value, out var force))
                result.ForceUpdate = force;

            var downloadUrl = configs.FirstOrDefault(c => c.Key == "Mobile.DownloadUrl");
            if (downloadUrl != null) result.DownloadUrl = downloadUrl.Value;

            // 媒体源 base（图片直出，前端拼相对键；下发以便换域名/切对象存储免发版）
            var mediaBaseUrl = configs.FirstOrDefault(c => c.Key == "Mobile.MediaBaseUrl");
            if (mediaBaseUrl != null) result.MediaBaseUrl = mediaBaseUrl.Value;

            // 改动说明（v1.5.2 僵尸清理）：原 Endpoints/Settings 自声明路由表与默认参数块已删——
            //   BFF MobileConfigDto 强类型反序列化从不透传，Taro 亦零消费；配置键 Mobile.MinVersion
            //   由 CheckVersion 查询直接读 SystemConfig，不经本 DTO
            // 改动说明：接入站点展示配置，移动端从 SystemConfigs 读取站点名称/备案号/客服联系方式
            var siteName = configs.FirstOrDefault(c => c.Key == "SiteName");
            if (siteName != null) result.SiteName = siteName.Value;

            // 改动说明：补充站点描述，使 SiteDescription 配置项具备真实消费方
            var siteDescription = configs.FirstOrDefault(c => c.Key == "SiteDescription");
            if (siteDescription != null) result.SiteDescription = siteDescription.Value;

            var beiAn = configs.FirstOrDefault(c => c.Key == "Site.BeiAn");
            if (beiAn != null) result.SiteBeiAn = beiAn.Value;

            var customerService = configs.FirstOrDefault(c => c.Key == "Site.CustomerService");
            if (customerService != null) result.CustomerService = customerService.Value;

            return result;
        }
    }
}
