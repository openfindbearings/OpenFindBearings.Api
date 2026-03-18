using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Application.Features.Sync.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Sync.Handlers
{
    /// <summary>
    /// 批量创建商家-轴承关联命令处理器
    /// </summary>
    public class BatchCreateMerchantBearingsCommandHandler : IRequestHandler<BatchCreateMerchantBearingsCommand, BatchResult>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<BatchCreateMerchantBearingsCommandHandler> _logger;

        public BatchCreateMerchantBearingsCommandHandler(
            IMerchantRepository merchantRepository,
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<BatchCreateMerchantBearingsCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _bearingRepository = bearingRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateMerchantBearingsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建商家-轴承关联，数量: {Count}", request.MerchantBearings.Count);

            var result = new BatchResult();

            foreach (var dto in request.MerchantBearings)
            {
                try
                {
                    // 查找商家
                    var merchants = await _merchantRepository.SearchAsync(
                        new Domain.Specifications.MerchantSearchParams
                        {
                            Keyword = dto.MerchantName,
                            PageSize = 10
                        }, cancellationToken);

                    var merchant = merchants.FirstOrDefault();
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
                        var existing = (await _merchantBearingRepository.GetByMerchantAsync(merchant.Id, cancellationToken))
                            .FirstOrDefault(mb => mb.BearingId == bearing.Id);

                        if (existing != null)
                        {
                            existing.UpdateMarketInfo(
                                dto.PriceDescription,
                                dto.StockDescription,
                                dto.MinOrderDescription,
                                dto.Remarks
                            );

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
                            dto.PriceDescription,
                            dto.StockDescription
                        );

                        if (!string.IsNullOrEmpty(dto.MinOrderDescription))
                        {
                            merchantBearing.UpdateMarketInfo(
                                dto.PriceDescription,
                                dto.StockDescription,
                                dto.MinOrderDescription,
                                dto.Remarks
                            );
                        }

                        if (!dto.IsOnSale)
                        {
                            merchantBearing.TakeOffShelf();
                        }

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
