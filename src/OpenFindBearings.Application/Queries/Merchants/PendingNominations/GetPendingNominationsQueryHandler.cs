using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.PendingNominations
{
    /// <summary>
    /// 待我接受的管理员提名查询处理器
    /// 按手机号匹配未过期的 Pending Nomination 邀请，并带出草稿商户名称供展示
    /// </summary>
    public class GetPendingNominationsQueryHandler : IRequestHandler<GetPendingNominationsQuery, List<PendingNominationDto>>
    {
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantRepository _merchantRepository;

        public GetPendingNominationsQueryHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantRepository merchantRepository)
        {
            _invitationRepository = invitationRepository;
            _merchantRepository = merchantRepository;
        }

        public async Task<List<PendingNominationDto>> Handle(GetPendingNominationsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Phone))
                return [];

            var invitations = await _invitationRepository.GetPendingNominationsByPhoneAsync(request.Phone, cancellationToken);

            // 逐条带出草稿商户名（邀请数量小，串行查询避免共享 DbContext 并发问题）
            var result = new List<PendingNominationDto>();
            foreach (var invitation in invitations)
            {
                var merchant = await _merchantRepository.GetByIdAsync(invitation.MerchantId, cancellationToken);
                if (merchant == null) continue;
                result.Add(new PendingNominationDto(
                    invitation.InvitationCode,
                    merchant.Id,
                    merchant.Name,
                    merchant.CompanyName,
                    invitation.CreatedAt));
            }
            return result;
        }
    }
}
