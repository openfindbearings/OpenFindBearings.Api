using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 创建商家命令
    /// </summary>
    public record CreateMerchantCommand : IRequest<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public MerchantType Type { get; init; }
        public string? ContactPerson { get; init; }
        public string? Phone { get; init; }
        public string? Mobile { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        public string? Description { get; init; }
        public string? BusinessScope { get; init; }
    }
}
