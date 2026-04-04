using MediatR;
using OpenFindBearings.Application.Features.Bearings.DTOs;

namespace OpenFindBearings.Application.Features.Bearings.Queries
{
    /// <summary>
    /// 通过型号获取轴承查询
    /// </summary>
    public record GetBearingByCodeQuery(string PartNumber) : IRequest<BearingDetailDto?>;
}
