using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingDocuments
{
    public record GetPendingDocumentsQuery : IRequest<PagedResult<PendingDocumentDto>>, IQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
