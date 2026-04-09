using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Admin.ApproveLicense
{
    /// <summary>
    /// 审核通过营业执照命令
    /// </summary>
    public record ApproveLicenseCommand : IRequest, ICommand
    {
        public Guid VerificationId { get; init; }
        public Guid ReviewedBy { get; init; }
        public string? Comment { get; init; }
    }
}
