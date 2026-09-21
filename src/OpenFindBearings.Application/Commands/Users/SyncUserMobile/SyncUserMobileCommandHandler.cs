using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Users.SyncUserMobile
{
    /// <summary>
    /// 手机号缓存同步处理器：仅 claim 非空且与缓存不同才更新（同值/无 claim 均跳过，防无谓 UPDATE 与误清空）
    /// </summary>
    public class SyncUserMobileCommandHandler : IRequestHandler<SyncUserMobileCommand>
    {
        private readonly IUserRepository _userRepository;

        public SyncUserMobileCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(SyncUserMobileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile))
                return;

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || user.Mobile == request.Mobile)
                return;

            user.SyncMobile(request.Mobile);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}
