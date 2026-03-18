using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.Features.Admin.Commands
{
    public record ApproveMerchantBearingCommand(Guid MerchantBearingId) : IRequest;
}
