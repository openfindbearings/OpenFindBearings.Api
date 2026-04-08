using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Merchants.CreateMerchant
{
    /// <summary>
    /// 创建商家命令处理器
    /// </summary>
    public class CreateMerchantCommandHandler : IRequestHandler<CreateMerchantCommand, Guid>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<CreateMerchantCommandHandler> _logger;

        public CreateMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<CreateMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始创建商家: {MerchantName}", request.Name);

            var contact = new ContactInfo(
                request.ContactPerson,
                request.Phone,
                request.Mobile,
                request.Email,
                request.Address
            );

            var merchant = new Merchant(
                request.Name,
                request.Type,
                contact
            );

            // ✅ 修改：传递所有6个参数
            merchant.UpdateBasicInfo(
                companyName: request.CompanyName,
                unifiedSocialCreditCode: request.UnifiedSocialCreditCode,  // 统一社会信用代码
                description: request.Description,
                businessScope: request.BusinessScope,
                logoUrl: request.LogoUrl,
                website: request.Website
            );

            await _merchantRepository.AddAsync(merchant, cancellationToken);

            _logger.LogInformation("商家创建成功: {MerchantId}, 名称: {MerchantName}",
                merchant.Id, merchant.Name);

            return merchant.Id;
        }
    }
}
