using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Merchants.SubmitDocument
{
    /// <summary>
    /// 提交商户证照材料审核命令（v2.7.0 由"提交营业执照"泛化：入驻后的换证/补授权书/厂房照走此通道）
    /// </summary>
    public record SubmitDocumentCommand : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 材料类型（营业执照/品牌授权书/厂房照片）
        /// </summary>
        public DocumentType Type { get; init; }

        /// <summary>
        /// 材料文件URL
        /// </summary>
        public string FileUrl { get; init; } = string.Empty;

        /// <summary>
        /// 提交人ID（商家员工）
        /// </summary>
        public Guid SubmittedBy { get; init; }
    }
}
