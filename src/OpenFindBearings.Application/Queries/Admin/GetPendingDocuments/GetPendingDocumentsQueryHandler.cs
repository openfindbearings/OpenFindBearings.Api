using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingDocuments
{
    public class GetPendingDocumentsQueryHandler : IRequestHandler<GetPendingDocumentsQuery, PagedResult<PendingDocumentDto>>
    {
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<GetPendingDocumentsQueryHandler> _logger;

        public GetPendingDocumentsQueryHandler(
            IMerchantDocumentRepository documentRepository,
            ILogger<GetPendingDocumentsQueryHandler> logger)
        {
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task<PagedResult<PendingDocumentDto>> Handle(GetPendingDocumentsQuery request, CancellationToken cancellationToken)
        {
            var result = await _documentRepository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);

            var items = result.Items.Select(item => item.ToDto()).ToList();

            return new PagedResult<PendingDocumentDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
