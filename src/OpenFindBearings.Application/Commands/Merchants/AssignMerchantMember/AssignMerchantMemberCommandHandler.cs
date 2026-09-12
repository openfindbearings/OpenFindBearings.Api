using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.AssignMerchantMember
{
    /// <summary>
    /// 平台指定商户成员命令处理器
    /// 已有成员行（含 Removed/Suspended）则复用恢复并设角色；无则新建 Active 成员行
    /// </summary>
    public class AssignMerchantMemberCommandHandler : IRequestHandler<AssignMerchantMemberCommand, Guid>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AssignMerchantMemberCommandHandler> _logger;

        public AssignMerchantMemberCommandHandler(
            IMerchantMemberRepository merchantMemberRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ILogger<AssignMerchantMemberCommandHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(AssignMerchantMemberCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
                throw new InvalidOperationException("商户不存在");

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("用户不存在");

            if (request.Role is not (MerchantMember.RoleMerchantAdmin or MerchantMember.RoleMerchantStaff))
                throw new InvalidOperationException("成员角色无效");

            // 含 Removed/Suspended 行的复用（P1 唯一约束设计：一人一商户仅一行）
            var existing = await _merchantMemberRepository.GetByUserAndMerchantAsync(request.UserId, request.MerchantId, cancellationToken);
            if (existing != null)
            {
                existing.Rejoin(request.Role);
                await _merchantMemberRepository.UpdateAsync(existing, cancellationToken);
                _logger.LogInformation("平台恢复商户成员: UserId={UserId}, MerchantId={MerchantId}, Role={Role}",
                    request.UserId, request.MerchantId, request.Role);
                return existing.Id;
            }

            var member = new MerchantMember(request.UserId, request.MerchantId, request.Role);
            await _merchantMemberRepository.AddAsync(member, cancellationToken);
            _logger.LogInformation("平台指定商户成员: UserId={UserId}, MerchantId={MerchantId}, Role={Role}",
                request.UserId, request.MerchantId, request.Role);
            return member.Id;
        }
    }
}
