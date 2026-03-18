using MediatR;
using OpenFindBearings.Application.Features.Bearings.DTOs;

namespace OpenFindBearings.Application.Features.Bearings.Queries
{
    /// <summary>
    /// 获取单个轴承查询
    /// </summary>
    public record GetBearingQuery(Guid Id) : IRequest<BearingDetailDto?>;
}
