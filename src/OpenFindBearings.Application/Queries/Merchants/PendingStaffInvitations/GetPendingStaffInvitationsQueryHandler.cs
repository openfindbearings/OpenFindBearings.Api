using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.PendingStaffInvitations
{
    /// <summary>
    /// 待我确认的员工邀请查询处理器：按手机号匹配 Pending Staff 邀请，
    /// 带出商户名与发起人昵称供横幅展示（数量小，串行查询共享 DbContext）
    /// </summary>
    public class GetPendingStaffInvitationsQueryHandler : IRequestHandler<GetPendingStaffInvitationsQuery, List<PendingStaffInvitationDto>>
    {
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;

        public GetPendingStaffInvitationsQueryHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository)
        {
            _invitationRepository = invitationRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task<List<PendingStaffInvitationDto>> Handle(GetPendingStaffInvitationsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Phone) && string.IsNullOrWhiteSpace(request.Email))
                return [];

            var invitations = await _invitationRepository.GetPendingStaffInvitationsByContactAsync(
                request.Phone, request.Email, cancellationToken);

            var result = new List<PendingStaffInvitationDto>();
            foreach (var invitation in invitations)
            {
                var merchant = await _merchantRepository.GetByIdAsync(invitation.MerchantId, cancellationToken);
                if (merchant == null) continue;

                var inviter = await _userRepository.GetByIdAsync(invitation.OperatorId, cancellationToken);

                result.Add(new PendingStaffInvitationDto(
                    invitation.Id,
                    merchant.Id,
                    merchant.Name,
                    invitation.Role ?? "MerchantStaff",
                    inviter?.Nickname,
                    invitation.CreatedAt));
            }
            return result;
        }
    }
}
