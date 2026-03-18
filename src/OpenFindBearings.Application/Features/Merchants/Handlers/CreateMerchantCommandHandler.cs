using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
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

            merchant.UpdateBasicInfo(
                request.CompanyName,
                request.Description,
                request.BusinessScope
            );

            await _merchantRepository.AddAsync(merchant, cancellationToken);

            _logger.LogInformation("商家创建成功: {MerchantId}, 名称: {MerchantName}",
                merchant.Id, merchant.Name);

            return merchant.Id;
        }
    }
}
