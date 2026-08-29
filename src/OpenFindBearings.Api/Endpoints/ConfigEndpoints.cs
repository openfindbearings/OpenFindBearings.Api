using MediatR;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Queries.Reliability;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 系统配置对外端点
    /// 职责：对外暴露可供内部服务（如 Sync）拉取的配置项
    /// </summary>
    public static class ConfigEndpoints
    {
        /// <summary>
        /// 映射系统配置端点
        /// </summary>
        public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/config").WithTags("系统配置");

            // 改动说明：新增可信度阈值端点，Sync 运行时拉取以替代自身 appsettings 默认值，实现后台可调
            group.MapGet("/reliability", async (IMediator mediator, HttpContext httpContext) =>
                {
                    var dto = await mediator.Send(new GetReliabilityConfigQuery());
                    return ApiResponseHelper.Ok(dto, httpContext: httpContext);
                })
                .RequireAuthorization()
                .WithName("GetReliabilityConfig")
                .WithSummary("获取可信度阈值配置（供 Sync 运行时拉取）");
        }
    }
}
