using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 更新商家命令
    /// </summary>
    public record UpdateMerchantCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; init; }
        public string? CompanyName { get; init; }
        public MerchantType? Type { get; init; }
        public string? ContactPerson { get; init; }
        public string? Phone { get; init; }
        public string? Mobile { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        public string? Description { get; init; }
        public string? BusinessScope { get; init; }
    }
}
