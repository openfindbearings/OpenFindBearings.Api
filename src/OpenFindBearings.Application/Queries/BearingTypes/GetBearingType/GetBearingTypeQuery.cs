using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.BearingTypes.GetBearingType
{
    /// <summary>
    /// 获取单个轴承类型查询
    /// </summary>
    public record GetBearingTypeQuery : IRequest<BearingTypeDto?>, IQuery
    {
        /// <summary>
        /// 轴承类型Id
        /// </summary>
        public Guid Id { get; set; }
    }
}
