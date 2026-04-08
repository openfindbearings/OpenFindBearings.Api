using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Admin.RejectLicense
{
    /// <summary>
    /// 审核拒绝营业执照命令
    /// </summary>
    public record RejectLicenseCommand : IRequest, ICommand
    {
        public Guid VerificationId { get; init; }
        public Guid ReviewedBy { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}
