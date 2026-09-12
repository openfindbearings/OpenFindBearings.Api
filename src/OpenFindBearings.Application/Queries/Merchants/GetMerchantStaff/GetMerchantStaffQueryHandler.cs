using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantStaff
{
    /// <summary>
    /// 获取商家员工列表查询处理器
    /// </summary>
    public class GetMerchantStaffQueryHandler : IRequestHandler<GetMerchantStaffQuery, List<MerchantStaffDto>>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<GetMerchantStaffQueryHandler> _logger;

        public GetMerchantStaffQueryHandler(
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<GetMerchantStaffQueryHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task<List<MerchantStaffDto>> Handle(
            GetMerchantStaffQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家成员列表: MerchantId={MerchantId}", request.MerchantId);

            // 改动说明：由 User.MerchantId + 全局角色改为按成员表查询，角色取自成员行，支持一人多商户
            var members = await _merchantMemberRepository.GetActiveByMerchantAsync(request.MerchantId, cancellationToken);

            return members.Select(m => new MerchantStaffDto
            {
                Id = m.UserId,
                Nickname = m.User?.Nickname ?? string.Empty,
                Avatar = m.User?.Avatar,
                Role = m.IsAdmin ? "管理员" : "员工",
                Status = m.Status.ToString()
            }).ToList();
        }
    }
}
