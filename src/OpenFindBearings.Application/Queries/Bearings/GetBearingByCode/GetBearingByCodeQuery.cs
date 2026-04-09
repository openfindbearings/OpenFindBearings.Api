using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingByCode
{
    /// <summary>
    /// 通过型号获取轴承查询
    /// </summary>
    public record GetBearingByCodeQuery : IRequest<BearingDetailDto?>, IQuery
    {
        /// <summary>
        /// 轴承型号
        /// </summary>
        public string CurrentCode { get; set; } = string.Empty;
    }
}
