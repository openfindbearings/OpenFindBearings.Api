using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Admin.BearingLeads
{
    /// <summary>
    /// 忽略型号线索命令
    /// 忽略后不再出现在待建档列表，但线索仍继续累积出现次数，可随时恢复
    /// </summary>
    /// <param name="Id">线索 ID</param>
    /// <param name="OperatorId">执行忽略的管理员用户 ID</param>
    public record IgnoreBearingLeadCommand(Guid Id, Guid? OperatorId) : IRequest, ICommand;

    /// <summary>
    /// 恢复型号线索命令
    /// 取消忽略，使线索重新回到待建档列表
    /// </summary>
    /// <param name="Id">线索 ID</param>
    public record RestoreBearingLeadCommand(Guid Id) : IRequest, ICommand;
}
