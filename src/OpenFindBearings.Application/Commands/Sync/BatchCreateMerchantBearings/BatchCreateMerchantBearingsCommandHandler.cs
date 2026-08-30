using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateMerchantBearings
{
    /// <summary>
    /// 批量创建商家-轴承关联命令处理器
    /// </summary>
    public class BatchCreateMerchantBearingsCommandHandler : IRequestHandler<BatchCreateMerchantBearingsCommand, BatchResult>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IPriceConfigProvider _priceConfigProvider;
        private readonly ILogger<BatchCreateMerchantBearingsCommandHandler> _logger;

        public BatchCreateMerchantBearingsCommandHandler(
            IMerchantRepository merchantRepository,
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IPriceConfigProvider priceConfigProvider,
            ILogger<BatchCreateMerchantBearingsCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _bearingRepository = bearingRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _priceConfigProvider = priceConfigProvider;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateMerchantBearingsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建商家-轴承关联，数量: {Count}", request.MerchantBearings.Count);

            var result = new BatchResult();

            // 改动说明：接入价格系统配置——在循环外读取一次（提供器内部有 5 分钟缓存），
            //           供批量记录提取数值价格并设置默认可见性，避免逐条查库
            var priceConfig = await _priceConfigProvider.GetAsync(cancellationToken);
            var defaultVisibility = PriceParser.ParseVisibility(priceConfig.DefaultVisibility);

            foreach (var dto in request.MerchantBearings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // 查找商家
                    var merchantsResult = await _merchantRepository.SearchAsync(
                        new Domain.Specifications.MerchantSearchParams
                        {
                            Keyword = dto.MerchantName,
                            PageSize = 10
                        }, cancellationToken);

                    // ✅ 修改：使用 merchantsResult.Items
                    var merchant = merchantsResult.Items.FirstOrDefault();
                    if (merchant == null)
                    {
                        result.AddFailed($"{dto.MerchantName}-{dto.BearingPartNumber}", $"商家不存在: {dto.MerchantName}");
                        continue;
                    }

                    // 查找轴承
                    var bearing = await _bearingRepository.GetByPartNumberAsync(dto.BearingPartNumber, cancellationToken);
                    if (bearing == null)
                    {
                        result.AddFailed($"{dto.MerchantName}-{dto.BearingPartNumber}", $"轴承不存在: {dto.BearingPartNumber}");
                        continue;
                    }

                    // 如果指定了品牌，验证品牌是否匹配
                    if (!string.IsNullOrEmpty(dto.BrandCode) && bearing.Brand?.Code != dto.BrandCode)
                    {
                        result.AddFailed($"{dto.MerchantName}-{dto.BearingPartNumber}",
                            $"品牌不匹配: 期望 {dto.BrandCode}, 实际 {bearing.Brand?.Code}");
                        continue;
                    }

                    // 检查关联是否已存在
                    var exists = await _merchantBearingRepository.ExistsAsync(merchant.Id, bearing.Id, cancellationToken);

                    if (exists)
                    {
                        // 更新现有关联
                        var merchantBearings = await _merchantBearingRepository.GetByMerchantAsync(merchant.Id, cancellationToken);
                        var existing = merchantBearings.FirstOrDefault(mb => mb.BearingId == bearing.Id);

                        if (existing != null)
                        {
                            existing.UpdateMarketInfo(
                                dto.Price,
                                dto.Stock,
                                dto.MinOrder,
                                dto.Remarks
                            );

                            // 改动说明：同步数值价格与可见性，使批量导入的数据同样可被排序与可见性控制覆盖
                            existing.SetNumericPrice(PriceParser.ExtractNumericPrice(dto.Price, priceConfig.ExtractPattern));
                            existing.SetPriceVisibility(defaultVisibility);

                            if (!dto.IsOnSale && existing.IsOnSale)
                            {
                                existing.TakeOffShelf();
                            }
                            else if (dto.IsOnSale && !existing.IsOnSale)
                            {
                                existing.PutOnShelf();
                            }

                            await _merchantBearingRepository.UpdateAsync(existing, cancellationToken);
                            result.AddSuccess($"{dto.MerchantName}-{dto.BearingPartNumber}", "updated", existing.Id);
                        }
                    }
                    else
                    {
                        // 创建新关联
                        var merchantBearing = new MerchantBearing(
                            merchant.Id,
                            bearing.Id,
                            dto.Price,
                            dto.Stock
                        );

                        if (!string.IsNullOrEmpty(dto.MinOrder))
                        {
                            merchantBearing.UpdateMarketInfo(
                                dto.Price,
                                dto.Stock,
                                dto.MinOrder,
                                dto.Remarks
                            );
                        }

                        // 改动说明：新建关联时按配置提取数值价格并设置默认可见性，
                        //           此前仅经 UpdateMarketInfo 赋值 PriceDescription，NumericPrice 恒为 null
                        merchantBearing.SetNumericPrice(PriceParser.ExtractNumericPrice(dto.Price, priceConfig.ExtractPattern));
                        merchantBearing.SetPriceVisibility(defaultVisibility);

                        if (!dto.IsOnSale)
                        {
                            merchantBearing.TakeOffShelf();
                        }

                        merchantBearing.SetDataSourceType(dto.DataSource switch
                        {
                            "Manual" => DataSourceType.Manual,
                            "FileImport" => DataSourceType.FileImport,
                            _ => DataSourceType.Crawler
                        });

                        await _merchantBearingRepository.AddAsync(merchantBearing, cancellationToken);
                        result.AddSuccess($"{dto.MerchantName}-{dto.BearingPartNumber}", "created", merchantBearing.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建关联失败: {Identifier}", $"{dto.MerchantName}-{dto.BearingPartNumber}");
                    result.AddFailed($"{dto.MerchantName}-{dto.BearingPartNumber}", ex.Message);
                }
            }

            return result;
        }
    }
}
