using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

        public UpdateUserProfileCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateUserProfileCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新用户资料: UserId={UserId}, Occupation={Occupation}",
                request.UserId, request.Occupation);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            // 更新基础资料
            if (request.Nickname != null || request.Avatar != null || request.Address != null)
            {
                user.UpdateProfile(request.Nickname, request.Avatar, request.Address);
            }

            // 更新用户画像
            if (request.Occupation.HasValue || request.CompanyName != null || request.Industry != null)
            {
                user.UpdateUserProfile(
                    occupation: request.Occupation ?? user.Occupation.GetValueOrDefault(),
                    companyName: request.CompanyName ?? user.CompanyName,
                    industry: request.Industry ?? user.Industry
                );
            }

            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("用户资料更新成功: UserId={UserId}", user.Id);
        }
    }
}
