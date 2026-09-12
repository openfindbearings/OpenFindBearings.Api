using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Merchants.PendingNominations
{
    /// <summary>
    /// 待我接受的管理员提名查询（G2：员工提名模式下，被提名人登录后按 JWT 手机号匹配）
    /// </summary>
    public record GetPendingNominationsQuery : IRequest<List<PendingNominationDto>>, IQuery
    {
        /// <summary>
        /// 被提名人手机号（取自 JWT phone_number claim，仅能查自己的邀请）
        /// </summary>
        public string Phone { get; init; } = string.Empty;
    }

    /// <summary>
    /// 待接受提名摘要
    /// </summary>
    public record PendingNominationDto(
        string InvitationCode,
        Guid MerchantId,
        string MerchantName,
        string? CompanyName,
        DateTime CreatedAt);
}
