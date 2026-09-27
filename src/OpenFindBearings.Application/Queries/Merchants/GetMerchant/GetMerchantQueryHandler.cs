using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchant
{
    /// <summary>
    /// 获取商家查询处理器
    /// </summary>
    public class GetMerchantQueryHandler : IRequestHandler<GetMerchantQuery, MerchantDetailDto?>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        // v2.6.0 商家主页：成员标记（成员区渲染依据）+ 集体任务达成数（勋章园卡通关史）
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantTaskRepository _taskRepository;
        private readonly ILogger<GetMerchantQueryHandler> _logger;

        public GetMerchantQueryHandler(
            IMerchantRepository merchantRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository memberRepository,
            IMerchantTaskRepository taskRepository,
            ILogger<GetMerchantQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _memberRepository = memberRepository;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task<MerchantDetailDto?> Handle(GetMerchantQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家详情: MerchantId={MerchantId}, IsAuthenticated={IsAuthenticated}",
                request.Id, request.IsAuthenticated);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
                return null;

            // 修复 B6：提名草稿（Draft）商户对 C 端不可见（资料未补全、未提交审核）
            if (merchant.Status == Domain.Enums.MerchantStatus.Draft)
                return null;

            // 获取商家在售产品列表
            var merchantBearings = await _merchantBearingRepository.GetOnSaleByMerchantAsync(request.Id, cancellationToken);

            var products = merchantBearings.Select(mb => mb.ToDto(request.IsAuthenticated)).ToList();

            var dto = merchant.ToDetailDto(products, request.IsAuthenticated);

            // v2.6.0 商家主页：集体任务累计达成数（公开信任信号，所有访客可见）
            dto.CompletedTaskCount = await _taskRepository.CountCompletionsAsync(request.Id, cancellationToken);

            // 成员标记与角色：仅登录用户查一次在职成员关系（非成员/未登录保持 false/null）
            if (request.UserId.HasValue)
            {
                var membership = await _memberRepository.GetActiveByUserAndMerchantAsync(
                    request.UserId.Value, request.Id, cancellationToken);
                if (membership != null)
                {
                    dto.IsMerchantMember = true;
                    dto.MemberRole = membership.Role;
                }
            }
            return dto;
        }
    }
}
