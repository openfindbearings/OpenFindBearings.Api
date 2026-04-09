using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateMerchants
{
    /// <summary>
    /// 批量创建商家命令处理器
    /// </summary>
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
                try
                {
                    // 检查商家是否已存在
                    var existingMerchantsResult = await _merchantRepository.SearchAsync(
                        new Domain.Specifications.MerchantSearchParams
                        {
                            Keyword = merchantDto.Name,
                            PageSize = 10
                        }, cancellationToken);

                    // ✅ 修改：使用 existingMerchantsResult.Items
                    var existing = existingMerchantsResult.Items.FirstOrDefault();

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

                    if (existing == null)
                    {
                        // 创建新商家
                        var contact = new ContactInfo(
                            merchantDto.ContactPerson,
                            merchantDto.Phone,
                            merchantDto.Mobile,
                            merchantDto.Email,
                            merchantDto.Address
                        );

                        var merchant = new Merchant(
                            merchantDto.Name,
                            (MerchantType)merchantDto.Type,
                            contact
                        );

                        // ✅ 修改：传递所有6个参数
                        merchant.UpdateBasicInfo(
                            companyName: merchantDto.CompanyName,
                            unifiedSocialCreditCode: merchantDto.UnifiedSocialCreditCode,
                            description: merchantDto.Description,
                            businessScope: merchantDto.BusinessScope,
                            logoUrl: merchantDto.LogoUrl,
                            website: merchantDto.Website
                        );

                        if (merchantDto.IsVerified)
                        {
                            merchant.Verify();
                        }

                        await _merchantRepository.AddAsync(merchant, cancellationToken);
                        result.AddSuccess(merchantDto.Name, "created", merchant.Id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        // ✅ 修改：传递所有6个参数
                        existing.UpdateBasicInfo(
                            companyName: merchantDto.CompanyName ?? existing.CompanyName,
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
    }
}
