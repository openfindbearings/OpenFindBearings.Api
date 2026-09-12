using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantApplication
{
    /// <summary>
    /// 查询用户在各商户的入驻状态查询处理器
    /// 按成员表返回用户全部在职商户的入驻进度
    /// </summary>
    public class GetMerchantApplicationQueryHandler : IRequestHandler<GetMerchantApplicationQuery, List<MerchantApplicationDto>>
    {
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetMerchantApplicationQueryHandler> _logger;

        public GetMerchantApplicationQueryHandler(
            IMerchantMemberRepository merchantMemberRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetMerchantApplicationQueryHandler> logger)
        {
            _merchantMemberRepository = merchantMemberRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<List<MerchantApplicationDto>> Handle(
            GetMerchantApplicationQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("查询入驻状态: UserId={UserId}", request.UserId);

            var members = await _merchantMemberRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);

            var result = new List<MerchantApplicationDto>();
            foreach (var member in members)
            {
                // 逐个顺序查询（EF 串行约束，不用 Task.WhenAll）
                var merchant = await _merchantRepository.GetByIdAsync(member.MerchantId, cancellationToken);
                if (merchant == null)
                    continue;

                result.Add(new MerchantApplicationDto
                {
                    MerchantId = merchant.Id,
                    MerchantName = merchant.Name,
                    Status = merchant.Status.ToString(),
                    RejectReason = merchant.Status == Domain.Enums.MerchantStatus.Suspended
                        ? merchant.SuspensionReason
                        : null,
                    Role = member.Role,
                    IsVerified = merchant.IsVerified
                });
            }

            return result;
        }
    }
}
