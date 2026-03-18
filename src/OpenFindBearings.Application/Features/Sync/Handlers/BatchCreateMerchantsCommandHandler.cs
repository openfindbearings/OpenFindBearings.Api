using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Application.Features.Sync.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Features.Sync.Handlers
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
                    var existingMerchants = await _merchantRepository.SearchAsync(
                        new Domain.Specifications.MerchantSearchParams
                        {
                            Keyword = merchantDto.Name,
                            PageSize = 10
                        }, cancellationToken);

                    var existing = existingMerchants.FirstOrDefault();

                    if (existing != null && request.Mode == Common.Enums.SyncMode.Create)
                    {
                        result.AddFailed(merchantDto.Name, "商家名称已存在");
                        continue;
                    }

                    if (existing == null && request.Mode == Common.Enums.SyncMode.Update)
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
                            (Domain.Enums.MerchantType)merchantDto.Type,
                            contact
                        );

                        merchant.UpdateBasicInfo(
                            merchantDto.CompanyName,
                            merchantDto.Description,
                            merchantDto.BusinessScope
                        );

                        if (merchantDto.IsVerified)
                        {
                            merchant.Verify();
                        }

                        await _merchantRepository.AddAsync(merchant, cancellationToken);
                        result.AddSuccess(merchantDto.Name, "created", merchant.Id);
                    }
                    else if (request.Mode == Common.Enums.SyncMode.Update || request.Mode == Common.Enums.SyncMode.Upsert)
                    {
                        // 更新现有商家
                        existing.UpdateBasicInfo(
                            merchantDto.CompanyName,
                            merchantDto.Description,
                            merchantDto.BusinessScope
                        );

                        var newContact = new ContactInfo(
                            merchantDto.ContactPerson ?? existing.Contact?.ContactPerson,
                            merchantDto.Phone ?? existing.Contact?.Phone,
                            merchantDto.Mobile ?? existing.Contact?.Mobile,
                            merchantDto.Email ?? existing.Contact?.Email,
                            merchantDto.Address ?? existing.Contact?.Address
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
