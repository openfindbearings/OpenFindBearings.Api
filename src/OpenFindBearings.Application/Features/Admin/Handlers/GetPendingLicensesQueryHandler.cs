using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Application.Features.Admin.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    public class GetPendingLicensesQueryHandler : IRequestHandler<GetPendingLicensesQuery, PagedResult<PendingLicenseDto>>
    {
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetPendingLicensesQueryHandler> _logger;

        public GetPendingLicensesQueryHandler(
            ILicenseVerificationRepository licenseRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ILogger<GetPendingLicensesQueryHandler> logger)
        {
            _licenseRepository = licenseRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<PendingLicenseDto>> Handle(GetPendingLicensesQuery request, CancellationToken cancellationToken)
        {
            var result = await _licenseRepository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);

            var items = new List<PendingLicenseDto>();
            foreach (var item in result.Items)
            {
                var merchant = await _merchantRepository.GetByIdAsync(item.MerchantId, cancellationToken);
                var submitter = await _userRepository.GetByIdAsync(item.SubmittedBy, cancellationToken);

                items.Add(new PendingLicenseDto
                {
                    Id = item.Id,
                    MerchantId = item.MerchantId,
                    MerchantName = merchant?.Name ?? "未知商家",
                    LicenseUrl = item.LicenseUrl,
                    Status = item.Status.ToString(),
                    SubmittedBy = item.SubmittedBy,
                    SubmitterName = submitter?.Nickname ?? "未知用户",
                    SubmittedAt = item.SubmittedAt
                });
            }

            return new PagedResult<PendingLicenseDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
