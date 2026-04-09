using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.Shared.Services;
using System.Reflection;

namespace OpenFindBearings.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // 注册MediatR（自动扫描所有 Handler 和 EventHandler）
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(assembly);
            });

            // 注册FluentValidation验证器
            services.AddValidatorsFromAssembly(assembly);

            // 添加验证行为
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // 注册 UnitOfWorkBehavior（后提交）
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

            // 业务自定义权限检查
            services.AddScoped<PermissionChecker>();

            return services;
        }
    }
}
