using MediatR;

namespace OpenFindBearings.Application.Features.Admin.Commands
{
    /// <summary>
    /// 审核拒绝营业执照命令
    /// </summary>
    public record RejectLicenseCommand : IRequest
    {
        public Guid VerificationId { get; init; }
        public Guid ReviewedBy { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}
