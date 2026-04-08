using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Merchants.UpdateMerchant
{
    /// <summary>
    /// 更新商家命令处理器
    /// </summary>
    public class UpdateMerchantCommandHandler : IRequestHandler<UpdateMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<UpdateMerchantCommandHandler> _logger;

        public UpdateMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<UpdateMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始更新商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            // ✅ 修改：更新基本信息 - 传递所有6个参数
            if (request.Name != null || request.CompanyName != null ||
                request.UnifiedSocialCreditCode != null ||
                request.Description != null || request.BusinessScope != null ||
                request.LogoUrl != null || request.Website != null)
            {
                merchant.UpdateBasicInfo(
                    companyName: request.CompanyName ?? merchant.CompanyName,
                    unifiedSocialCreditCode: request.UnifiedSocialCreditCode ?? merchant.UnifiedSocialCreditCode,
                    description: request.Description ?? merchant.Description,
                    businessScope: request.BusinessScope ?? merchant.BusinessScope,
                    logoUrl: request.LogoUrl ?? merchant.LogoUrl,
                    website: request.Website ?? merchant.Website
                );
            }

            // 更新名称（如果有单独更新名称的方法）
            if (request.Name != null)
            {
                merchant.UpdateName(request.Name);
            }

            // 更新联系方式
            if (request.ContactPerson != null || request.Phone != null ||
                request.Mobile != null || request.Email != null || request.Address != null)
            {
                var newContact = new ContactInfo(
                    contactPerson: request.ContactPerson ?? merchant.Contact?.ContactPerson,
                    phone: request.Phone ?? merchant.Contact?.Phone,
                    mobile: request.Mobile ?? merchant.Contact?.Mobile,
                    email: request.Email ?? merchant.Contact?.Email,
                    address: request.Address ?? merchant.Contact?.Address
                );
                merchant.UpdateContact(newContact);
            }

            // 更新类型
            if (request.Type.HasValue)
            {
                // 注意：Merchant 实体可能需要添加 UpdateType 方法
                // merchant.UpdateType(request.Type.Value);
            }

            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家更新成功: {MerchantId}", merchant.Id);
        }
    }
}
