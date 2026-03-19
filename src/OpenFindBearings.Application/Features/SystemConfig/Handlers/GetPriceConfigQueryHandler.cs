using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.SystemConfig.DTOs;
using OpenFindBearings.Application.Features.SystemConfig.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.SystemConfig.Handlers
{
    /// <summary>
    /// 获取价格配置查询处理器
    /// </summary>
    public class GetPriceConfigQueryHandler : IRequestHandler<GetPriceConfigQuery, PriceConfigDto>
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<GetPriceConfigQueryHandler> _logger;

        public GetPriceConfigQueryHandler(
            ISystemConfigRepository systemConfigRepository,
            ILogger<GetPriceConfigQueryHandler> logger)
        {
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task<PriceConfigDto> Handle(
            GetPriceConfigQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取价格配置");

            var configs = await _systemConfigRepository.GetAllAsync(cancellationToken);

            var result = new PriceConfigDto();

            // 读取各项价格配置
            var defaultVisibility = configs.FirstOrDefault(c => c.Key == "Price.DefaultVisibility");
            if (defaultVisibility != null)
            {
                result.DefaultVisibility = defaultVisibility.Value;
            }

            var showNegotiable = configs.FirstOrDefault(c => c.Key == "Price.ShowNegotiableLabel");
            if (showNegotiable != null && bool.TryParse(showNegotiable.Value, out var showNegotiableValue))
            {
                result.ShowNegotiableLabel = showNegotiableValue;
            }

            var numericForSorting = configs.FirstOrDefault(c => c.Key == "Price.NumericForSorting");
            if (numericForSorting != null && bool.TryParse(numericForSorting.Value, out var numericValue))
            {
                result.NumericForSorting = numericValue;
            }

            var extractPattern = configs.FirstOrDefault(c => c.Key == "Price.ExtractPattern");
            if (extractPattern != null)
            {
                result.ExtractPattern = extractPattern.Value;
            }

            return result;
        }
    }
}
