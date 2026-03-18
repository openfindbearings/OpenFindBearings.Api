using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
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

            // 更新基本信息
            if (request.Name != null || request.CompanyName != null ||
                request.Description != null || request.BusinessScope != null)
            {
                merchant.UpdateBasicInfo(
                    request.CompanyName,
                    request.Description,
                    request.BusinessScope
                );
            }

            // 更新联系方式
            if (request.ContactPerson != null || request.Phone != null ||
                request.Mobile != null || request.Email != null || request.Address != null)
            {
                var newContact = new ContactInfo(
                    request.ContactPerson ?? merchant.Contact?.ContactPerson,
                    request.Phone ?? merchant.Contact?.Phone,
                    request.Mobile ?? merchant.Contact?.Mobile,
                    request.Email ?? merchant.Contact?.Email,
                    request.Address ?? merchant.Contact?.Address
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
