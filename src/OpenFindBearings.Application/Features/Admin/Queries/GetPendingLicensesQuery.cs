using MediatR;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Admin.Queries
{
    public record GetPendingLicensesQuery : IRequest<PagedResult<PendingLicenseDto>>
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
