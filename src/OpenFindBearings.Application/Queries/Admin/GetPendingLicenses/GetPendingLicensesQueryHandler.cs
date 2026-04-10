using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingLicenses
{
    public class GetPendingLicensesQueryHandler : IRequestHandler<GetPendingLicensesQuery, PagedResult<PendingLicenseDto>>
    {
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly ILogger<GetPendingLicensesQueryHandler> _logger;

        public GetPendingLicensesQueryHandler(
            ILicenseVerificationRepository licenseRepository,
            ILogger<GetPendingLicensesQueryHandler> logger)
        {
            _licenseRepository = licenseRepository;
            _logger = logger;
        }

        public async Task<PagedResult<PendingLicenseDto>> Handle(GetPendingLicensesQuery request, CancellationToken cancellationToken)
        {
            var result = await _licenseRepository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);

            var items = result.Items.Select(item => item.ToDto()).ToList();

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
