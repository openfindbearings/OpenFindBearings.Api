using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.Services;
using System.Reflection;

namespace OpenFindBearings.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // 注册FluentValidation验证器（在MediatR之前）
            services.AddValidatorsFromAssembly(assembly);

            // 注册MediatR（自动扫描所有 Handler 和 EventHandler）
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.Lifetime = ServiceLifetime.Scoped;

                // 注意：MediatR 内部注册顺序与执行顺序相反
                // 后注册的先执行，所以先写 UnitOfWork，再写 Validation

                // 2️ 后执行（因为先注册）
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

                // 1️ 先执行（因为后注册）
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // 在外面按顺序注册 Behaviors（先注册的先执行）
            //// 添加验证行为
            //services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            //// 注册 UnitOfWorkBehavior（后提交）
            //services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

            // 业务自定义权限检查
            services.AddScoped<PermissionChecker>();

            return services;
        }
    }
}
