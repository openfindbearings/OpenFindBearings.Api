using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Domain.Enums;
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

            // 改动说明（v2.9.0 字段锁定）：入驻生效后企业主体信息（企业名称/统一社会信用代码）
            //   与营业执照绑定、是平台认证依据，不允许自助修改（换主体=重新入驻）；
            //   仅在请求真正试图改成不同值时拒绝，未传/传同值放行（兼容部分字段更新场景）
            if (merchant.Status == MerchantStatus.Active)
            {
                if (request.CompanyName != null && request.CompanyName != merchant.CompanyName)
                    throw new InvalidOperationException("企业名称入驻后不可修改，如需变更请联系平台");
                if (request.UnifiedSocialCreditCode != null && request.UnifiedSocialCreditCode != merchant.UnifiedSocialCreditCode)
                    throw new InvalidOperationException("统一社会信用代码入驻后不可修改，如需变更请联系平台");
            }

            // ✅ 修改：更新基本信息 - 传递所有6个参数
            if (request.Name != null || request.CompanyName != null ||
                request.EnglishName != null ||
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

                if (request.EnglishName != null)
                    merchant.SetEnglishName(request.EnglishName);
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

            // 覆盖保护：人工维护（Admin 编辑/商户自改）过的数据标记为非爬虫来源，
            // 使后续爬虫批量同步跳过，避免人工修改被爬虫数据覆盖
            merchant.SetDataSource(DataSource.FromManual());

            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家更新成功: {MerchantId}", merchant.Id);
        }
    }
}
