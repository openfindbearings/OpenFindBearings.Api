using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.BatchCheckVerified
{
    public class BatchCheckVerifiedQueryHandler : IRequestHandler<BatchCheckVerifiedQuery, List<string>>
    {
        private readonly IMerchantRepository _merchantRepository;

        public BatchCheckVerifiedQueryHandler(IMerchantRepository merchantRepository)
        {
            _merchantRepository = merchantRepository;
        }

        public async Task<List<string>> Handle(BatchCheckVerifiedQuery request, CancellationToken cancellationToken)
        {
            if (request.MerchantNames == null || request.MerchantNames.Count == 0)
                return [];

            return await _merchantRepository.GetVerifiedNamesAsync(
                request.MerchantNames.Distinct().ToList(), cancellationToken);
        }
    }
}
