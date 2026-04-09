using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Merchants.Commands
{
    /// <summary>
    /// 更新商家命令
    /// </summary>
    public record UpdateMerchantCommand : IRequest, ICommand
    {
        public Guid Id { get; set; }

        // 基本信息
        public string? Name { get; set; }
        public string? CompanyName { get; set; }
        public string? UnifiedSocialCreditCode { get; set; } 
        public MerchantType? Type { get; set; }

        // 描述信息
        public string? Description { get; set; }
        public string? BusinessScope { get; set; }

        // 品牌形象
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }

        // 联系方式
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}
