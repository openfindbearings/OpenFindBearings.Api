using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateMerchants
{
    public class BatchCreateMerchantsCommandHandler : IRequestHandler<BatchCreateMerchantsCommand, BatchResult>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<BatchCreateMerchantsCommandHandler> _logger;

        public BatchCreateMerchantsCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<BatchCreateMerchantsCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateMerchantsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建商家，数量: {Count}, 模式: {Mode}",
                request.Merchants.Count, request.Mode);

            var result = new BatchResult();

            foreach (var merchantDto in request.Merchants)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var existing = await _merchantRepository.GetByNameAsync(merchantDto.Name, cancellationToken);

                    if (existing != null && request.Mode == SyncMode.Create)
                    {
                        result.AddFailed(merchantDto.Name, "商家名称已存在");
                        continue;
                    }

                    if (existing == null && request.Mode == SyncMode.Update)
                    {
                        result.AddFailed(merchantDto.Name, "商家不存在");
                        continue;
                    }

                    if (existing != null && request.Mode != SyncMode.Create)
                    {
                        if (existing.DataSource != null && existing.DataSource.SourceType != DataSourceType.Crawler)
                        {
                            result.AddSkipped(merchantDto.Name, "非爬虫数据，跳过覆盖保护");
                            _logger.LogDebug("跳过覆盖商家: {Name}, 来源: {Source}", merchantDto.Name, existing.DataSource.SourceType);
                            continue;
                        }

                        if (existing.DataSource == null && merchantDto.DataSource != null)
                        {
                            existing.SetDataSource(CreateDataSource(merchantDto.DataSource, merchantDto.SourceSite));
                        }
                    }

                    if (existing == null)
                    {
                        var contact = new ContactInfo(
                            contactPerson: merchantDto.ContactPerson,
                            phone: merchantDto.Phone,
                            mobile: merchantDto.Mobile,
                            email: merchantDto.Email,
                            address: merchantDto.Address
                        );

                        var merchant = new Merchant(
                            merchantDto.Name,
                            (MerchantType)merchantDto.Type,
                            contact
                        );

                        merchant.UpdateBasicInfo(
                            companyName: merchantDto.EnglishName,
                            unifiedSocialCreditCode: merchantDto.UnifiedSocialCreditCode,
                            description: merchantDto.Description,
                            businessScope: merchantDto.BusinessScope,
                            logoUrl: merchantDto.LogoUrl,
                            website: merchantDto.Website
                        );

                        if (merchantDto.DataSource != null)
                        {
                            merchant.SetDataSource(CreateDataSource(merchantDto.DataSource, merchantDto.SourceSite));
                        }

                        if (merchantDto.IsVerified)
                        {
                            merchant.Verify();
                        }

                        await _merchantRepository.AddAsync(merchant, cancellationToken);
                        result.AddSuccess(merchantDto.Name, "created", merchant.Id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        existing.UpdateBasicInfo(
                            companyName: merchantDto.EnglishName ?? existing.CompanyName,
                            unifiedSocialCreditCode: merchantDto.UnifiedSocialCreditCode ?? existing.UnifiedSocialCreditCode,
                            description: merchantDto.Description ?? existing.Description,
                            businessScope: merchantDto.BusinessScope ?? existing.BusinessScope,
                            logoUrl: merchantDto.LogoUrl ?? existing.LogoUrl,
                            website: merchantDto.Website ?? existing.Website
                        );

                        var newContact = new ContactInfo(
                            contactPerson: merchantDto.ContactPerson ?? existing.Contact?.ContactPerson,
                            phone: merchantDto.Phone ?? existing.Contact?.Phone,
                            mobile: merchantDto.Mobile ?? existing.Contact?.Mobile,
                            email: merchantDto.Email ?? existing.Contact?.Email,
                            address: merchantDto.Address ?? existing.Contact?.Address
                        );
                        existing.UpdateContact(newContact);

                        await _merchantRepository.UpdateAsync(existing, cancellationToken);
                        result.AddSuccess(merchantDto.Name, "updated", existing.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建商家失败: {MerchantName}", merchantDto.Name);
                    result.AddFailed(merchantDto.Name, ex.Message);
                }
            }

            _logger.LogInformation("批量创建商家完成，成功: {SuccessCount}, 失败: {FailCount}",
                result.SuccessCount, result.FailCount);

            return result;
        }

        private static DataSource CreateDataSource(string? dataSource, string? sourceSite)
        {
            var sourceType = dataSource ?? "manual";
            var importedBy = sourceSite;

            if (sourceType.Equals("crawler", StringComparison.OrdinalIgnoreCase))
                return DataSource.FromCrawler(importedBy ?? "unknown");
            if (sourceType.Equals("api", StringComparison.OrdinalIgnoreCase))
                return DataSource.FromApi(importedBy ?? "ApiSync");
            if (sourceType.Equals("fileimport", StringComparison.OrdinalIgnoreCase))
                return DataSource.FromFileImport(importedBy);
            return DataSource.FromManual(importedBy);
        }
    }
}
