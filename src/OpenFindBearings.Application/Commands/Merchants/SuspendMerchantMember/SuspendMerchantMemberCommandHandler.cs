using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.SuspendMerchantMember
{
    /// <summary>
    /// 停用商户成员命令处理器
    /// 守卫：操作人须为在职管理员；不能停用自己；不能停用最后一名在职管理员
    /// </summary>
    public class SuspendMerchantMemberCommandHandler : IRequestHandler<SuspendMerchantMemberCommand>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<SuspendMerchantMemberCommandHandler> _logger;

        public SuspendMerchantMemberCommandHandler(
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<SuspendMerchantMemberCommandHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task Handle(SuspendMerchantMemberCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("停用成员: UserId={UserId}, MerchantId={MerchantId}, OperatorId={OperatorId}",
                request.UserId, request.MerchantId, request.OperatorId);

            var operatorMember = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorId, request.MerchantId, cancellationToken);
            if (operatorMember == null || !operatorMember.IsAdmin)
            {
                throw new UnauthorizedAccessException("需要商户管理员权限");
            }

            if (request.UserId == request.OperatorId)
            {
                throw new InvalidOperationException("不能停用自己，请先交接给其他管理员");
            }

            var target = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (target == null)
            {
                throw new InvalidOperationException("目标成员不是该商户在职成员");
            }

            // ≥1 管理员守卫：最后一名在职管理员不可被停用
            if (target.IsAdmin)
            {
                var adminCount = await _merchantMemberRepository.CountActiveAdminsAsync(request.MerchantId, cancellationToken);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("商户至少需要一名在职管理员，请先指定新的管理员");
                }
            }

            target.Suspend();
            await _merchantMemberRepository.UpdateAsync(target, cancellationToken);

            _logger.LogInformation("成员已停用: UserId={UserId}, MerchantId={MerchantId}", request.UserId, request.MerchantId);
        }
    }
}
