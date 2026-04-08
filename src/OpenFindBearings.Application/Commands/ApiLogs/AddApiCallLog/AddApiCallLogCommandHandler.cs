using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.ApiLogs.AddApiCallLog
{
    public class AddApiCallLogCommandHandler : IRequestHandler<AddApiCallLogCommand>
    {
        private readonly IApiCallLogRepository _apiCallLogRepository;
        private readonly ILogger<AddApiCallLogCommandHandler> _logger;

        public AddApiCallLogCommandHandler(
            IApiCallLogRepository apiCallLogRepository,
            ILogger<AddApiCallLogCommandHandler> logger)
        {
            _apiCallLogRepository = apiCallLogRepository;
            _logger = logger;
        }

        public async Task Handle(AddApiCallLogCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _apiCallLogRepository.AddAsync(request.Log, cancellationToken);
            }
            catch (Exception ex)
            {
                // 日志记录失败不应影响主流程
                _logger.LogError(ex, "记录API调用日志失败");
            }
        }
    }
}
