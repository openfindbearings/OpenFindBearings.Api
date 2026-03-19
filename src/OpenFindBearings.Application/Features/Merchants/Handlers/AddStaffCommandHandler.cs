using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 添加员工命令处理器
    /// </summary>
    public class AddStaffCommandHandler : IRequestHandler<AddStaffCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<AddStaffCommandHandler> _logger;

        public AddStaffCommandHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            ILogger<AddStaffCommandHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(AddStaffCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("添加员工: MerchantId={MerchantId}, Email={Email}, OperatorId={OperatorId}",
                request.MerchantId, request.Email, request.OperatorId);

            // 检查操作人权限
            var operator_ = await _userRepository.GetByIdAsync(request.OperatorId, cancellationToken);
            if (operator_ == null || operator_.MerchantId != request.MerchantId)
            {
                throw new UnauthorizedAccessException("您无权添加员工");
            }

            // 检查商家是否存在
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 检查用户是否已存在
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                // TODO: 发送邀请邮件，创建用户
                throw new InvalidOperationException("用户不存在，请先邀请用户注册");
            }

            // 关联用户到商家
            user.AssignToMerchant(request.MerchantId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // 分配角色
            var roleName = request.Role ?? "MerchantStaff";
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role != null)
            {
                await _userRoleRepository.AddUserToRoleAsync(user.Id, role.Id, cancellationToken);
            }

            return user.Id;
        }
    }
}
