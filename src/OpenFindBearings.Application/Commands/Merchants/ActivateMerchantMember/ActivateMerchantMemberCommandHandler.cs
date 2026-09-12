using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.ActivateMerchantMember
{
    /// <summary>
    /// 恢复被停用的商户成员命令处理器
    /// 守卫：操作人须为在职管理员
    /// </summary>
    public class ActivateMerchantMemberCommandHandler : IRequestHandler<ActivateMerchantMemberCommand>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<ActivateMerchantMemberCommandHandler> _logger;

        public ActivateMerchantMemberCommandHandler(
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<ActivateMerchantMemberCommandHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task Handle(ActivateMerchantMemberCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("恢复成员: UserId={UserId}, MerchantId={MerchantId}, OperatorId={OperatorId}",
                request.UserId, request.MerchantId, request.OperatorId);

            var operatorMember = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorId, request.MerchantId, cancellationToken);
            if (operatorMember == null || !operatorMember.IsAdmin)
            {
                throw new UnauthorizedAccessException("需要商户管理员权限");
            }

            // 含已停用/已移除行，恢复后转为在职
            var target = await _merchantMemberRepository.GetByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (target == null)
            {
                throw new InvalidOperationException("目标成员不存在");
            }

            target.Reactivate();
            await _merchantMemberRepository.UpdateAsync(target, cancellationToken);

            _logger.LogInformation("成员已恢复: UserId={UserId}, MerchantId={MerchantId}", request.UserId, request.MerchantId);
        }
    }
}
