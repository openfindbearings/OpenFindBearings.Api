using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Admin.GetMerchantDocuments
{
    /// <summary>
    /// 查询指定商户全部证照材料命令（v2.7.0 新增，Admin 审批抽屉"申请材料"区数据源；
    /// 不并入 C 端商家详情，避免授权书等敏感材料 URL 对外泄露）
    /// </summary>
    public record GetMerchantDocumentsQuery : IRequest<List<PendingDocumentDto>>, IQuery
    {
        /// <summary>目标商户ID</summary>
        public Guid MerchantId { get; init; }
    }
}
