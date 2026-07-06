using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Sync.Commands;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateBrands
{
    /// <summary>
    /// 批量创建品牌命令处理器
    /// </summary>
    public class BatchCreateBrandsCommandHandler : IRequestHandler<BatchCreateBrandsCommand, BatchResult>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<BatchCreateBrandsCommandHandler> _logger;

        public BatchCreateBrandsCommandHandler(
            IBrandRepository brandRepository,
            ILogger<BatchCreateBrandsCommandHandler> logger)
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateBrandsCommand request, CancellationToken cancellationToken)
        {
            var result = new BatchResult();

            foreach (var brandDto in request.Brands)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var existingBrand = await _brandRepository.GetByCodeAsync(brandDto.Code, cancellationToken);

                    // DataSource 保护逻辑：非爬虫数据不可被覆盖
                    if (existingBrand != null && existingBrand.DataSource?.SourceType != DataSourceType.Crawler)
                    {
                        result.AddSkipped(brandDto.Code, "非爬虫数据，跳过覆盖保护");
                        _logger.LogDebug("跳过覆盖: {Code}, 来源: {Source}", brandDto.Code, existingBrand.DataSource?.SourceType);
                        continue;
                    }

                    if (existingBrand != null && request.Mode == SyncMode.Create)
                    {
                        result.AddFailed(brandDto.Code, $"品牌已存在: {brandDto.Code}");
                        continue;
                    }

                    if (existingBrand == null && request.Mode == SyncMode.Update)
                    {
                        result.AddFailed(brandDto.Code, $"品牌不存在: {brandDto.Code}");
                        continue;
                    }

                    if (existingBrand == null)
                    {
                        // 创建新品牌 - 使用映射方法处理 Level
                        var level = MapToBrandLevel(brandDto.Level);
                        var brand = new Brand(brandDto.Code, brandDto.Name, level);

                        // 设置可选属性
                        if (!string.IsNullOrEmpty(brandDto.Country) || !string.IsNullOrEmpty(brandDto.LogoUrl))
                            brand.UpdateDetails(brandDto.Country, brandDto.LogoUrl);

                        SetDataSource(brand, brandDto);
                        await _brandRepository.AddAsync(brand, cancellationToken);
                        result.AddSuccess(brandDto.Code, "created", brand.Id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        // 更新现有品牌
                        if (!string.IsNullOrEmpty(brandDto.Name))
                            existingBrand.UpdateName(brandDto.Name);

                        if (!string.IsNullOrEmpty(brandDto.Country) || !string.IsNullOrEmpty(brandDto.LogoUrl))
                            existingBrand.UpdateDetails(brandDto.Country, brandDto.LogoUrl);

                        if (!string.IsNullOrEmpty(brandDto.Level))
                        {
                            var level = MapToBrandLevel(brandDto.Level);
                            existingBrand.UpdateLevel(level);
                        }

                        await _brandRepository.UpdateAsync(existingBrand, cancellationToken);
                        result.AddSuccess(brandDto.Code, "updated", existingBrand.Id);
                    }
                }
                catch (Exception ex)
                {
                    result.AddFailed(brandDto.Code, ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// 将字符串映射到 BrandLevel 枚举
        /// </summary>
        private static void SetDataSource(Brand brand, SyncBrandDto dto)
        {
            var sourceType = dto.DataSource ?? "manual";
            var importedBy = dto.SourceSite;

            if (sourceType.Equals("crawler", StringComparison.OrdinalIgnoreCase))
                brand.SetDataSource(DataSource.FromCrawler(importedBy ?? "unknown"));
            else if (sourceType.Equals("api", StringComparison.OrdinalIgnoreCase))
                brand.SetDataSource(DataSource.FromApi(importedBy ?? "ApiSync"));
            else if (sourceType.Equals("file", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("fileimport", StringComparison.OrdinalIgnoreCase))
                brand.SetDataSource(DataSource.FromFileImport(importedBy));
            else if (sourceType.Equals("seeddata", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("seed", StringComparison.OrdinalIgnoreCase))
                brand.SetDataSource(DataSource.FromSeedData());
            else
                brand.SetDataSource(DataSource.FromManual(importedBy));
        }

        private BrandLevel MapToBrandLevel(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return BrandLevel.InternationalStandard; // 默认国际标准品牌

            return level.ToLower().Trim() switch
            {
                // 国际一线
                "international_premium" or "premium" or "top" or "skf" or "fag" or "nsk"
                    => BrandLevel.InternationalPremium,

                // 国际标准
                "international_standard" or "standard" or "timken" or "ntn"
                    => BrandLevel.InternationalStandard,

                // 国产一线
                "domestic_premium" or "domestic" or "hrb" or "zwz" or "lyc"
                    => BrandLevel.DomesticPremium,

                // 国产二线
                "domestic_standard" or "domestic_second"
                    => BrandLevel.DomesticStandard,

                // 经济型
                "economy" or "budget" or "cheap"
                    => BrandLevel.Economy,

                // 默认
                _ => BrandLevel.InternationalStandard
            };
        }
    }
}
