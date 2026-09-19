using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.VerifyMerchant
{
    /// <summary>
    /// 认证商家命令处理器
    /// v2.7.0：认证口径升级——该商家类型必备材料须全部"审核通过"方可认证
    /// （全类型营业执照、授权经销商另需品牌授权书），杜绝无据认证
    /// </summary>
    public class VerifyMerchantCommandHandler : IRequestHandler<VerifyMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<VerifyMerchantCommandHandler> _logger;

        public VerifyMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantDocumentRepository documentRepository,
            ILogger<VerifyMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task Handle(VerifyMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始认证商家: {MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            // 改动说明（v2.7.0）：认证前校验必备材料均已批准；未通过原因直接回给审核人，避免点开才发现
            var documents = await _documentRepository.GetByMerchantIdAsync(merchant.Id, cancellationToken);
            var missing = Application.DTOs.DocumentRequirements.RequiredTypes(merchant.Type)
                .Where(t => !documents.Any(d => d.Type == t && d.Status == DocumentStatus.Approved))
                .Select(Application.DTOs.DocumentRequirements.DisplayName)
                .ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException($"缺少已审核通过的必备材料：{string.Join("、", missing)}");
            }

            merchant.Verify(request.VerifiedBy);
            // 改动说明：认证=平台人工核实确认，此后该商户资料属"人工维护"，来源置 Manual，
            //   使其不再被 Sync 爬虫同步覆盖（覆盖保护以 DataSource 为键，与认领轴解耦）。
            merchant.SetDataSource(OpenFindBearings.Domain.ValueObjects.DataSource.FromManual(request.VerifiedBy));
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("商家认证成功: {MerchantId}", request.Id);
        }
    }
}
