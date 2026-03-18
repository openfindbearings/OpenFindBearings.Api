using MediatR;

namespace OpenFindBearings.Application.Features.History.Commands
{
    /// <summary>
    /// 记录商家浏览历史命令
    /// </summary>
    public record RecordMerchantViewCommand(
        Guid MerchantId,
        string UserId  // AuthUserId
    ) : IRequest;
}
