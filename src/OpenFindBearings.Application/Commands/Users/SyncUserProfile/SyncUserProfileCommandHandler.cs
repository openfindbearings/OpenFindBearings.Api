using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Users.SyncUserProfile
{
    /// <summary>
    /// 用户资料缓存同步处理器：手机号有值且不同才刷新；昵称仅空值补写（用户自改值永不覆盖）。
    /// 两值均无需变更时零写入（成员列表高频请求不做无效 UPDATE）。
    /// </summary>
    public class SyncUserProfileCommandHandler : IRequestHandler<SyncUserProfileCommand>
    {
        private readonly IUserRepository _userRepository;

        public SyncUserProfileCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(SyncUserProfileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) && string.IsNullOrWhiteSpace(request.Nickname))
                return;

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return;

            var changed = false;
            if (!string.IsNullOrWhiteSpace(request.Mobile) && user.Mobile != request.Mobile)
            {
                user.SyncMobile(request.Mobile);
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(request.Nickname) && string.IsNullOrWhiteSpace(user.Nickname))
            {
                user.SyncNickname(request.Nickname);
                changed = true;
            }
            if (changed)
            {
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }
    }
}
