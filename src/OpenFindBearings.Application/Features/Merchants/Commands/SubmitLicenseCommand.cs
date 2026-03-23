using MediatR;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 提交营业执照审核命令
    /// </summary>
    public record SubmitLicenseCommand : IRequest<Guid>
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 营业执照文件URL
        /// </summary>
        public string LicenseUrl { get; init; } = string.Empty;

        /// <summary>
        /// 提交人ID（商家员工）
        /// </summary>
        public Guid SubmittedBy { get; init; }
    }
}
