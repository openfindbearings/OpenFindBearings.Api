using MediatR;

namespace OpenFindBearings.Application.Queries.Merchants.BatchCheckVerified
{
    public class BatchCheckVerifiedQuery : IRequest<List<string>>
    {
        public List<string> MerchantNames { get; set; } = [];
    }
}
