using MediatR;

namespace OpenFindBearings.Application.Features.Admin.Commands
{
    /// <summary>
    /// 审核通过营业执照命令
    /// </summary>
    public record ApproveLicenseCommand : IRequest
    {
        public Guid VerificationId { get; init; }
        public Guid ReviewedBy { get; init; }
        public string? Comment { get; init; }
    }
}
