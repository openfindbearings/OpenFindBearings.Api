using MediatR;
using OpenFindBearings.Application.Services;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Application.Services;
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
        // v1.34.0：资料完善度达标一次性积分奖励（bizId 绑信用代码幂等）
        private readonly IPointsService _pointsService;

        public UpdateMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<UpdateMerchantCommandHandler> logger,
            IPointsService pointsService)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
            _pointsService = pointsService;
        }

        public async Task Handle(UpdateMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始更新商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            // 改动说明（v2.11.0 调整）：Active 商户主体信息守卫细化——
            //   企业名称：一律锁定（与执照绑定，换主体=重新入驻）；
            //   信用代码：允许"空→一次性补录"（历史选填时代的空值商户需能补），
            //     非空后改不同值拒绝；
            //   商家类型：锁定——类型决定材料矩阵与认证标准（授权经销商必品牌授权书），
            //     自助改类型=绕过审核换标准，变更需求走平台人工。
            if (merchant.Status == MerchantStatus.Active)
            {
                if (request.CompanyName != null && request.CompanyName != merchant.CompanyName)
                    throw new InvalidOperationException("企业名称入驻后不可修改，如需变更请联系平台");
                if (request.UnifiedSocialCreditCode != null && request.UnifiedSocialCreditCode != merchant.UnifiedSocialCreditCode)
                {
                    if (!string.IsNullOrWhiteSpace(merchant.UnifiedSocialCreditCode))
                        throw new InvalidOperationException("统一社会信用代码已录入，不可再次修改，如需变更请联系平台");
                    // 空值补录：按入驻同口径校验格式
                    var creditCodeError = Application.DTOs.DocumentRequirements.ValidateCreditCode(request.UnifiedSocialCreditCode);
                    if (creditCodeError != null)
                        throw new InvalidOperationException(creditCodeError);
                }
                if (request.Type.HasValue && request.Type.Value != merchant.Type)
                    throw new InvalidOperationException("商家类型入驻后不可修改，如需变更请联系平台");
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

            // 更新类型（非 Active 可改；Active 已被上方守卫拦截）
            // 改动说明（v2.11.0）：原调用被注释、类型编辑实为假动作，现真正接通
            if (request.Type.HasValue && request.Type.Value != merchant.Type)
            {
                merchant.UpdateType(request.Type.Value);
            }

            // 覆盖保护：人工维护（Admin 编辑/商户自改）过的数据标记为非爬虫来源，
            // 使后续爬虫批量同步跳过，避免人工修改被爬虫数据覆盖
            merchant.SetDataSource(DataSource.FromManual());

            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 改动说明（v1.34.0 积分防刷设计）：资料完善度首次达标一次性奖励——
            //   口径=联系人+电话+简介+地址四项非空（logo 可选不计），且必须有信用代码
            //   （走认领台账绑信用代码：删店重入驻/换账号保存都不重复发）；无信用代码暂不发，
            //   补码后再保存即触发；GrantOneTimeAsync 吞异常，积分失败不影响资料保存主流程
            var contact = merchant.Contact;
            if (request.UserId.HasValue
                && !string.IsNullOrWhiteSpace(merchant.UnifiedSocialCreditCode)
                && !string.IsNullOrWhiteSpace(contact?.ContactPerson)
                && !string.IsNullOrWhiteSpace(contact?.Phone)
                && !string.IsNullOrWhiteSpace(merchant.Description)
                && !string.IsNullOrWhiteSpace(contact?.Address))
            {
                await _pointsService.GrantOneTimeAsync(
                    request.UserId.Value,
                    PointTransaction.TypeMerchantProfileComplete,
                    $"credit:{merchant.UnifiedSocialCreditCode.Trim().ToUpperInvariant()}:profile",
                    $"完善商户「{merchant.Name}」资料",
                    cancellationToken);
            }

            _logger.LogInformation("商家更新成功: {MerchantId}", merchant.Id);
        }
    }
}
