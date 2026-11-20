using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    /// <summary>
    /// 安全目录种子：权限点、平台角色与角色权限绑定。
    /// 改动说明：这些数据原先只在种子主流程里创建，被 "SystemConfigs 非空即跳过" 的判据连坐，
    ///   全新库启动后既没有 Admin 角色也没有任意一条权限绑定，Admin 后台所有端点返回 403。
    ///   现改为每次启动幂等校验：只补缺失项，绝不删除已有绑定（其它模块补充的权限与绑定依赖此行为）。
    /// </summary>
    public static partial class SeedData
    {
        /// <summary>权限点定义（资源.动作）。</summary>
        private static readonly (string Name, string Description)[] PermissionDefinitions =
        [
            ("dashboard.view", "查看仪表盘"),
            ("bearing.view", "查看轴承"),
            ("bearing.create", "创建轴承"),
            ("bearing.edit", "编辑轴承"),
            ("bearing.delete", "删除轴承"),
            ("brand.view", "查看品牌"),
            ("brand.create", "创建品牌"),
            ("brand.edit", "编辑品牌"),
            ("brand.delete", "删除品牌"),
            ("type.view", "查看型号"),
            ("type.create", "创建型号"),
            ("type.edit", "编辑型号"),
            ("type.delete", "删除型号"),
            ("interchange.view", "查看替代关系"),
            ("interchange.manage", "维护替代关系"),
            ("merchant.view", "查看商家"),
            ("merchant.manage", "编辑商家"),
            ("merchant.verify", "认证审核商家"),
            ("merchant.detach", "解除商家归属"),
            ("merchant.grade.manage", "调整商家卡级"),
            ("data.restore", "恢复已删数据"),
            ("data.harddelete", "彻底删除数据"),
            ("correction.review", "审核纠错"),
            ("sourcing.view", "查看寻货"),
            ("sourcing.manage", "治理寻货"),
            ("user.view", "查看用户"),
            ("user.ban", "封禁与解禁用户"),
            ("user.manage", "管理用户账号"),
            ("user.assign", "分配平台角色"),
            ("role.manage", "管理角色"),
            ("permission.view", "查看权限清单"),
            ("system.view", "查看系统配置"),
            ("system.manage", "修改系统配置"),
            ("points.manage", "配置积分任务"),
            ("audit.view", "查看审计日志"),
            ("feedback.view", "查看意见反馈"),
            ("feedback.edit", "处理与回复意见反馈"),
        ];

        /// <summary>平台角色定义（机器名、说明、是否系统内置、中文显示名）。</summary>
        private static readonly (string Name, string Description, bool IsSystem, string DisplayName)[] RoleDefinitions =
        [
            ("Admin", "平台管理员", true, "管理员"),
            ("Operator", "平台操作员", true, "操作员"),
            ("Auditor", "平台审计员", true, "审计员"),
            ("Individual", "App端注册用户（无后台权限）", true, "注册用户"),
        ];

        /// <summary>操作员权限集：日常数据维护与审核，不含删除、恢复、解除归属与提权类键。</summary>
        private static readonly string[] OperatorPermissions =
        [
            "dashboard.view",
            "bearing.view", "bearing.create", "bearing.edit",
            "brand.view", "brand.create", "brand.edit",
            "type.view", "type.create", "type.edit",
            "interchange.view", "interchange.manage",
            "merchant.view", "merchant.manage", "merchant.verify", "merchant.grade.manage",
            "correction.review", "sourcing.view", "sourcing.manage",
            "feedback.view", "feedback.edit",
            "user.view", "user.ban",
        ];

        /// <summary>审计员权限集：只读监察全部查看类键，无任何写权限。</summary>
        private static readonly string[] AuditorPermissions =
        [
            "dashboard.view", "bearing.view", "brand.view", "type.view", "merchant.view",
            "interchange.view", "correction.review", "sourcing.view", "feedback.view",
            "user.view", "permission.view", "system.view", "audit.view",
        ];

        /// <summary>
        /// 幂等补全权限点、角色与角色权限绑定。
        /// 只新增缺失项，不删除已有行：其它模块补充的权限不会在下次启动时被清掉。
        /// </summary>
        private static async Task EnsureSecurityCatalogAsync(ApplicationDbContext context, ILogger logger)
        {
            var existingPermissions = await context.Permissions.ToListAsync();
            var permissionsByName = existingPermissions
                .GroupBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            var permissionAdded = 0;
            foreach (var def in PermissionDefinitions)
            {
                if (permissionsByName.ContainsKey(def.Name))
                {
                    continue;
                }

                var permission = new Permission(def.Name, def.Description);
                context.Permissions.Add(permission);
                permissionsByName[def.Name] = permission;
                permissionAdded++;
            }

            if (permissionAdded > 0)
            {
                await context.SaveChangesAsync();
            }

            var existingRoles = await context.Roles.ToListAsync();
            var rolesByName = existingRoles
                .GroupBy(r => r.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            var roleAdded = 0;
            foreach (var def in RoleDefinitions)
            {
                if (rolesByName.ContainsKey(def.Name))
                {
                    continue;
                }

                var role = new Role(def.Name, def.Description, def.IsSystem, def.DisplayName);
                context.Roles.Add(role);
                rolesByName[def.Name] = role;
                roleAdded++;
            }

            if (roleAdded > 0)
            {
                await context.SaveChangesAsync();
            }

            var existingBindings = await context.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .ToListAsync();
            var bindingSet = existingBindings
                .Select(b => (b.RoleId, b.PermissionId))
                .ToHashSet();

            var bindingsToAdd = new List<RolePermission>();

            if (rolesByName.TryGetValue("Admin", out var adminRole))
            {
                foreach (var permission in permissionsByName.Values)
                {
                    if (bindingSet.Add((adminRole.Id, permission.Id)))
                    {
                        bindingsToAdd.Add(new RolePermission(adminRole.Id, permission.Id));
                    }
                }
            }

            if (rolesByName.TryGetValue("Operator", out var operatorRole))
            {
                foreach (var name in OperatorPermissions)
                {
                    if (!permissionsByName.TryGetValue(name, out var permission))
                    {
                        continue;
                    }

                    if (bindingSet.Add((operatorRole.Id, permission.Id)))
                    {
                        bindingsToAdd.Add(new RolePermission(operatorRole.Id, permission.Id));
                    }
                }
            }

            if (rolesByName.TryGetValue("Auditor", out var auditorRole))
            {
                foreach (var name in AuditorPermissions)
                {
                    if (!permissionsByName.TryGetValue(name, out var permission))
                    {
                        continue;
                    }

                    if (bindingSet.Add((auditorRole.Id, permission.Id)))
                    {
                        bindingsToAdd.Add(new RolePermission(auditorRole.Id, permission.Id));
                    }
                }
            }

            if (bindingsToAdd.Count > 0)
            {
                await context.RolePermissions.AddRangeAsync(bindingsToAdd);
                await context.SaveChangesAsync();
            }

            if (permissionAdded > 0 || roleAdded > 0 || bindingsToAdd.Count > 0)
            {
                logger.LogInformation(
                    "安全目录已补全：权限 +{PermissionAdded}、角色 +{RoleAdded}、授权 +{BindingAdded}",
                    permissionAdded, roleAdded, bindingsToAdd.Count);
            }
        }
    }
}