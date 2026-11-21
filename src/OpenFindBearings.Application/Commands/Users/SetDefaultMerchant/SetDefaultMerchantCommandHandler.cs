using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Users.SetDefaultMerchant
{
    /// <summary>
    /// 设置默认商户处理器（v1.59.17）：先校验在职成员关系，再写入用户偏好（不存在则创建）。
    /// </summary>
    public class SetDefaultMerchantCommandHandler : IRequestHandler<SetDefaultMerchantCommand>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IUserPreferenceRepository _userPreferenceRepository;
        private readonly ILogger<SetDefaultMerchantCommandHandler> _logger;

        public SetDefaultMerchantCommandHandler(
            IMerchantMemberRepository merchantMemberRepository,
            IUserPreferenceRepository userPreferenceRepository,
            ILogger<SetDefaultMerchantCommandHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _userPreferenceRepository = userPreferenceRepository;
            _logger = logger;
        }

        /// <summary>校验成员关系并写入默认商户</summary>
        public async Task Handle(SetDefaultMerchantCommand request, CancellationToken cancellationToken)
        {
            var member = await _merchantMemberRepository
                .GetActiveByUserAndMerchantAsync(request.UserId, request.MerchantId, cancellationToken);
            if (member is null)
            {
                throw new InvalidOperationException("只能把自己在职的商户设为默认");
            }

            var preference = await _userPreferenceRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            if (preference is null)
            {
                preference = new UserPreference(request.UserId);
                preference.SetDefaultMerchant(request.MerchantId);
                await _userPreferenceRepository.AddAsync(preference, cancellationToken);
            }
            else
            {
                preference.SetDefaultMerchant(request.MerchantId);
                await _userPreferenceRepository.UpdateAsync(preference, cancellationToken);
            }

            _logger.LogInformation("默认商户已更新: UserId={UserId}, MerchantId={MerchantId}", request.UserId, request.MerchantId);
        }
    }
}