using MediatR;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetMerchantDocuments
{
    /// <summary>
    /// 商户证照材料查询处理器：返回该商户全部材料（含历史已审），按提交时间正序，
    /// 供 Admin 入驻审批抽屉展示与认证口径核对
    /// </summary>
    public class GetMerchantDocumentsQueryHandler : IRequestHandler<GetMerchantDocumentsQuery, List<PendingDocumentDto>>
    {
        private readonly IMerchantDocumentRepository _documentRepository;

        public GetMerchantDocumentsQueryHandler(IMerchantDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<List<PendingDocumentDto>> Handle(GetMerchantDocumentsQuery request, CancellationToken cancellationToken)
        {
            var documents = await _documentRepository.GetByMerchantIdAsync(request.MerchantId, cancellationToken);
            return documents
                .OrderBy(d => d.SubmittedAt)
                .Select(d => d.ToDto())
                .ToList();
        }
    }
}
