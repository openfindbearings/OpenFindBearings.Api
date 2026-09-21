using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantStaff
{
    /// <summary>
    /// 获取商家员工列表查询处理器
    /// 改动说明（v2.9.0）：在职成员之外合并该商户待确认的员工邀请（Status=Invited 行），
    ///   管理员可直观看到"谁还没接受"并提供撤销；邀请行无 UserId，操作走 InvitationId
    /// </summary>
    public class GetMerchantStaffQueryHandler : IRequestHandler<GetMerchantStaffQuery, List<MerchantStaffDto>>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ILogger<GetMerchantStaffQueryHandler> _logger;

        public GetMerchantStaffQueryHandler(
            IMerchantMemberRepository merchantMemberRepository,
            IStaffInvitationRepository invitationRepository,
            ILogger<GetMerchantStaffQueryHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _invitationRepository = invitationRepository;
            _logger = logger;
        }

        public async Task<List<MerchantStaffDto>> Handle(
            GetMerchantStaffQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家成员列表: MerchantId={MerchantId}", request.MerchantId);

            // 改动说明：由 User.MerchantId + 全局角色改为按成员表查询，角色取自成员行，支持一人多商户
            var members = await _merchantMemberRepository.GetActiveByMerchantAsync(request.MerchantId, cancellationToken);

            var result = members.Select(m => new MerchantStaffDto
            {
                Id = m.UserId,
                Nickname = m.User?.Nickname ?? string.Empty,
                Avatar = m.User?.Avatar,
                Role = m.IsAdmin ? "管理员" : "员工",
                Status = m.Status.ToString(),
                // 改动说明（v1.5.2）：后端标记本人行（成员管理 UI 隐藏自操作，守卫在前端不可靠）
                IsSelf = request.CurrentUserId.HasValue && m.UserId == request.CurrentUserId.Value
            }).ToList();

            // 待确认邀请行（v2.9.0）：仓储无"按商户查待确认员工邀请"方法，取该商户全部 Pending Staff 邀请内存过滤（量小）
            var pendingInvitations = await _invitationRepository.GetPendingStaffInvitationsByMerchantAsync(request.MerchantId, cancellationToken);
            foreach (var inv in pendingInvitations)
            {
                result.Add(new MerchantStaffDto
                {
                    Id = Guid.Empty,
                    // 展示被邀联系方式（管理员自己输入的手机号，不属隐私泄露）
                    Nickname = inv.Phone ?? inv.Email ?? "待确认",
                    Role = inv.Role == "MerchantAdmin" ? "管理员（待确认）" : "员工（待确认）",
                    Status = "Invited",
                    InvitationId = inv.Id
                });
            }

            return result;
        }
    }
}
