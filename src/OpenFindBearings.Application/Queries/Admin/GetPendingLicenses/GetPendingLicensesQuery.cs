using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingLicenses
{
    public record GetPendingLicensesQuery : IRequest<PagedResult<PendingLicenseDto>>, IQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
