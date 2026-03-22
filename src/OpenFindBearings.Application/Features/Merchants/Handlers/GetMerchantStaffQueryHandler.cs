using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 获取商家员工列表查询处理器
    /// </summary>
    public class GetMerchantStaffQueryHandler : IRequestHandler<GetMerchantStaffQuery, List<MerchantStaffDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMerchantStaffQueryHandler> _logger;

        public GetMerchantStaffQueryHandler(
            IUserRepository userRepository,
            ILogger<GetMerchantStaffQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<MerchantStaffDto>> Handle(
            GetMerchantStaffQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家员工列表: MerchantId={MerchantId}", request.MerchantId);

            var staff = await _userRepository.GetByMerchantIdAsync(request.MerchantId, cancellationToken);

            return staff.Select(s => new MerchantStaffDto
            {
                Id = s.Id,
                Nickname = s.Nickname ?? string.Empty,
                Avatar = s.Avatar,
                Role = GetUserRole(s)  // 从角色系统获取
            }).ToList();
        }

        /// <summary>
        /// 获取用户在商家中的角色（按权限优先级）
        /// </summary>
        private string GetUserRole(User user)
        {
            var roles = user.UserRoles
                .Select(ur => ur.Role?.Name)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            if (!roles.Any())
                return "员工";

            // 按权限优先级排序：管理员 > 员工
            if (roles.Contains("MerchantAdmin"))
                return "管理员";

            return "员工";
        }
    }
}
