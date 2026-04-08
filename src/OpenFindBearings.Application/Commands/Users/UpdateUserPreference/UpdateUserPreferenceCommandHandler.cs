using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using System.Text.Json;

namespace OpenFindBearings.Application.Commands.Users.UpdateUserPreference
{
    /// <summary>
    /// 更新用户偏好命令处理器
    /// 处理用户各类偏好的更新，如果偏好不存在则自动创建
    /// </summary>
    public class UpdateUserPreferenceCommandHandler : IRequestHandler<UpdateUserPreferenceCommand>
    {
        private readonly IUserPreferenceRepository _preferenceRepository;
        private readonly ILogger<UpdateUserPreferenceCommandHandler> _logger;

        public UpdateUserPreferenceCommandHandler(
            IUserPreferenceRepository preferenceRepository,
            ILogger<UpdateUserPreferenceCommandHandler> logger)
        {
            _preferenceRepository = preferenceRepository;
            _logger = logger;
        }

        /// <summary>
        /// 处理更新用户偏好命令
        /// </summary>
        public async Task Handle(UpdateUserPreferenceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新用户偏好: UserId={UserId}", request.UserId);

            // 获取或创建用户偏好
            var preference = await _preferenceRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (preference == null)
            {
                preference = new UserPreference(request.UserId);
                await _preferenceRepository.AddAsync(preference, cancellationToken);
                _logger.LogDebug("用户偏好不存在，已创建: UserId={UserId}", request.UserId);
            }

            // 更新地区偏好
            if (request.Province != null || request.City != null)
            {
                preference.UpdateRegionPreference(request.Province, request.City);
                _logger.LogDebug("地区偏好已更新: UserId={UserId}, Province={Province}, City={City}",
                    request.UserId, request.Province, request.City);
            }

            // 更新品牌偏好
            if (request.BrandIds != null)
            {
                preference.UpdatePreferredBrands(request.BrandIds);
                _logger.LogDebug("品牌偏好已更新: UserId={UserId}, BrandIds={BrandIds}",
                    request.UserId, string.Join(",", request.BrandIds));
            }

            // 更新轴承类型偏好
            if (request.BearingTypeIds != null)
            {
                preference.UpdatePreferredBearingTypes(request.BearingTypeIds);
                _logger.LogDebug("轴承类型偏好已更新: UserId={UserId}, TypeIds={TypeIds}",
                    request.UserId, string.Join(",", request.BearingTypeIds));
            }

            // 更新价格区间偏好
            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                preference.UpdatePriceRange(request.MinPrice, request.MaxPrice);
                _logger.LogDebug("价格区间偏好已更新: UserId={UserId}, MinPrice={MinPrice}, MaxPrice={MaxPrice}",
                    request.UserId, request.MinPrice, request.MaxPrice);
            }

            // 更新通知偏好
            if (request.EmailNotificationEnabled.HasValue ||
                request.SmsNotificationEnabled.HasValue ||
                request.WeChatNotificationEnabled.HasValue)
            {
                preference.UpdateNotificationPreferences(
                    request.EmailNotificationEnabled,
                    request.SmsNotificationEnabled,
                    request.WeChatNotificationEnabled);
                _logger.LogDebug("通知偏好已更新: UserId={UserId}", request.UserId);
            }

            await _preferenceRepository.UpdateAsync(preference, cancellationToken);

            _logger.LogInformation("用户偏好更新成功: UserId={UserId}", request.UserId);
        }
    }
}
