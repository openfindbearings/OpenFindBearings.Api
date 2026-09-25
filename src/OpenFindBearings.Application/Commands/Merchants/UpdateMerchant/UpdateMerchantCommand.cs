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

        /// <summary>
        /// 操作人用户 ID（v1.34.0：资料完善度达标奖励归属判定；null=非商户侧操作如 Admin 后台编辑，不触发发奖）
        /// </summary>
        public Guid? UserId { get; set; }

        // 基本信息
        public string? Name { get; set; }
        public string? CompanyName { get; set; }
        public string? EnglishName { get; set; }
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
