using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Admin.BearingLeads
{
    /// <summary>
    /// 忽略型号线索命令处理器
    /// </summary>
    public class IgnoreBearingLeadCommandHandler : IRequestHandler<IgnoreBearingLeadCommand>
    {
        private readonly IBearingLeadRepository _bearingLeadRepository;
        private readonly ILogger<IgnoreBearingLeadCommandHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public IgnoreBearingLeadCommandHandler(
            IBearingLeadRepository bearingLeadRepository,
            ILogger<IgnoreBearingLeadCommandHandler> logger)
        {
            _bearingLeadRepository = bearingLeadRepository;
            _logger = logger;
        }

        /// <summary>
        /// 执行忽略：线索不存在时报错，已忽略时保持幂等
        /// </summary>
        public async Task Handle(IgnoreBearingLeadCommand request, CancellationToken cancellationToken)
        {
            var lead = await _bearingLeadRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException("型号线索不存在");

            if (lead.IsIgnored)
                return;

            lead.Ignore(request.OperatorId);
            await _bearingLeadRepository.UpdateAsync(lead, cancellationToken);
            _logger.LogInformation("型号线索已忽略: {LeadId} 型号 {PartNumber}", lead.Id, lead.PartNumber);
        }
    }

    /// <summary>
    /// 恢复型号线索命令处理器
    /// </summary>
    public class RestoreBearingLeadCommandHandler : IRequestHandler<RestoreBearingLeadCommand>
    {
        private readonly IBearingLeadRepository _bearingLeadRepository;
        private readonly ILogger<RestoreBearingLeadCommandHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RestoreBearingLeadCommandHandler(
            IBearingLeadRepository bearingLeadRepository,
            ILogger<RestoreBearingLeadCommandHandler> logger)
        {
            _bearingLeadRepository = bearingLeadRepository;
            _logger = logger;
        }

        /// <summary>
        /// 执行恢复：线索不存在时报错，未忽略时保持幂等
        /// </summary>
        public async Task Handle(RestoreBearingLeadCommand request, CancellationToken cancellationToken)
        {
            var lead = await _bearingLeadRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException("型号线索不存在");

            if (!lead.IsIgnored)
                return;

            lead.Restore();
            await _bearingLeadRepository.UpdateAsync(lead, cancellationToken);
            _logger.LogInformation("型号线索已恢复: {LeadId} 型号 {PartNumber}", lead.Id, lead.PartNumber);
        }
    }
}
