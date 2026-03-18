using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
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

        public async Task<List<MerchantStaffDto>> Handle(GetMerchantStaffQuery request, CancellationToken cancellationToken)
        {
            var staff = await _userRepository.GetByMerchantIdAsync(request.MerchantId, cancellationToken);

            return staff.Select(s => new MerchantStaffDto
            {
                Id = s.Id,
                Nickname = s.Nickname ?? string.Empty,
                Email = s.Email,
                Phone = s.Phone,
                Avatar = s.Avatar,
                Role = "员工" // TODO: 从角色系统获取
            }).ToList();
        }
    }
}
