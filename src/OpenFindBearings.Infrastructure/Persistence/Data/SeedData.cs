using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(IServiceProvider provider, ILogger logger, bool isDevelopment)
        {
            try
            {
                await using var context = provider.GetRequiredService<ApplicationDbContext>();

                if (isDevelopment)
                {
                    //await context.Database.EnsureDeletedAsync();
                }

                await context.Database.MigrateAsync();

                await ExecuteAsync(context, logger, isDevelopment);

                // 幂等补全：已有库在执行 ExecuteAsync 时因配置表非空会跳过种子，这里补全本次新增的配置键
                await EnsureConfigKeysAsync(context);

                logger.LogInformation("数据库初始化成功");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "数据库初始化失败");
            }
        }

        private static async Task ExecuteAsync(ApplicationDbContext context, ILogger logger, bool isDevelopment)
        {
            if (await context.SystemConfigs.AnyAsync())
            {
                return;
            }

            // ============ 1. 基础字典数据（开发环境） ============

            #region 基础字典数据
            var brands = new List<Brand>();
            var bearingTypes = new List<BearingType>();

            if (isDevelopment)
            {
                // 添加品牌
                brands.AddRange(
                [
                    new("SKF", "SKF", BrandLevel.InternationalPremium),
                    new("FAG", "FAG", BrandLevel.InternationalPremium),
                    new("NSK", "NSK", BrandLevel.InternationalPremium),
                    new("HRB", "HRB", BrandLevel.DomesticPremium),
                    new("ZWZ", "ZWZ", BrandLevel.DomesticPremium),
                    new("LYC", "LYC", BrandLevel.DomesticPremium)
                ]);

                await context.Brands.AddRangeAsync(brands);
                await context.SaveChangesAsync();

                // 添加轴承类型
                bearingTypes.AddRange(
                [
                    new("DGBB", "深沟球轴承", "最常用的滚动轴承，主要承受径向载荷"),
                    new("ACBB", "角接触球轴承", "可同时承受径向和轴向载荷"),
                    new("SRB", "调心滚子轴承", "具有调心功能，适用于重载"),
                    new("TRB", "圆锥滚子轴承", "可承受径向和轴向联合载荷")
                ]);

                await context.BearingTypes.AddRangeAsync(bearingTypes);
                await context.SaveChangesAsync();
            }
            #endregion

            // ============ 2. 角色和权限（必须，无论开发/生产） ============

            #region 角色和权限
            // 创建权限
            var permissions = new List<Permission>
            {
                new("bearing.view", "查看产品"),
                new("bearing.create", "创建产品"),
                new("bearing.edit", "编辑产品"),
                new("bearing.delete", "删除产品"),
                new("merchant.view", "查看商家"),
                new("merchant.verify", "认证商家"),
                new("merchant.manage", "管理商家"),
                new("correction.submit", "提交纠错"),
                new("correction.review", "审核纠错"),
                new("favorite.bearing", "收藏轴承"),
                new("favorite.merchant", "关注商家"),
                new("user.manage", "管理用户"),
                new("role.manage", "管理角色"),
                new("dashboard.view", "查看仪表盘"),
                new("audit.view", "查看审计日志"),
                new("system.view", "查看系统配置"),
                new("system.manage", "管理系统配置"),
                new("data.restore", "恢复数据"),
                new("data.harddelete", "彻底删除"),
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            // 创建角色
            var roles = new List<Role>
            {
                new("Admin", "平台管理员", true),
                new("MerchantAdmin", "商家管理员", true),
                new("MerchantStaff", "商家员工", true),
                new("Individual", "个人用户", true)
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            // 分配权限给角色
            var adminRole = roles.First(r => r.Name == "Admin");
            var merchantAdminRole = roles.First(r => r.Name == "MerchantAdmin");
            var merchantStaffRole = roles.First(r => r.Name == "MerchantStaff");
            var individualRole = roles.First(r => r.Name == "Individual");

            var rolePermissions = new List<RolePermission>();

            // Admin 拥有所有权限
            foreach (var permission in permissions)
            {
                rolePermissions.Add(new RolePermission(adminRole.Id, permission.Id));
            }

            // MerchantAdmin 拥有商家管理权限
            rolePermissions.AddRange([
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "bearing.view").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "bearing.create").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "bearing.edit").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "merchant.view").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "merchant.manage").Id),
            ]);

            // MerchantStaff 拥有查看权限
            rolePermissions.AddRange([
                new(merchantStaffRole.Id, permissions.First(p => p.Name == "bearing.view").Id),
                new(merchantStaffRole.Id, permissions.First(p => p.Name == "merchant.view").Id),
            ]);

            // Individual 拥有基本权限
            rolePermissions.AddRange([
                new(individualRole.Id, permissions.First(p => p.Name == "bearing.view").Id),
                new(individualRole.Id, permissions.First(p => p.Name == "correction.submit").Id),
                new(individualRole.Id, permissions.First(p => p.Name == "favorite.bearing").Id),
                new(individualRole.Id, permissions.First(p => p.Name == "favorite.merchant").Id),
            ]);

            await context.RolePermissions.AddRangeAsync(rolePermissions);
            await context.SaveChangesAsync();
            #endregion

            // ============ 3. 用户数据 ============

            #region 用户数据
            var users = new List<User>();
            var userRoles = new List<UserRole>();

            if (isDevelopment)
            {
                var merchant1 = new User("auth-merchant-001", RegistrationSource.Mobile, null, "张经理");
                var merchant2 = new User("auth-merchant-002", RegistrationSource.Mobile, null, "李经理");
                var customer1 = new User("auth-customer-001", RegistrationSource.Mobile, null, "王先生");
                var customer2 = new User("auth-customer-002", RegistrationSource.Mobile, null, "赵女士");

                users.AddRange([merchant1, merchant2, customer1, customer2]);

                userRoles.AddRange([
                    new UserRole(merchant1.Id, merchantAdminRole.Id),
                    new UserRole(merchant2.Id, merchantAdminRole.Id),
                    new UserRole(customer1.Id, individualRole.Id),
                    new UserRole(customer2.Id, individualRole.Id),
                ]);
            }

            if (users.Count > 0)
            {
                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();
            }
            #endregion

            // ============ 4. 轴承产品数据（仅开发环境） ============

            #region 轴承产品数据

            var bearings = new List<Bearing>();

            if (isDevelopment && bearingTypes.Any() && brands.Any())
            {
                // SKF 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    partNumber: "6205",
                    bearingTypeId: bearingTypes[0].Id,
                    bearingType: bearingTypes[0].Name,
                    innerDiameter: 25, outerDiameter: 52, width: 15,
                    brandId: brands[0].Id,
                    weight: 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6206",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    30, 62, 16, brands[0].Id, 0.15m));

                bearings.Add(Bearing.CreateBearing(
                    "6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[0].Id, 0.17m));

                bearings.Add(Bearing.CreateBearing(
                    "6310",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    50, 110, 27, brands[0].Id, 0.85m));

                // FAG 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[1].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[1].Id, 0.17m));

                bearings.Add(Bearing.CreateBearing(
                    "7205-B",
                    bearingTypes[1].Id, bearingTypes[1].Name,
                    25, 52, 15, brands[1].Id, 0.13m));

                // NSK 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[2].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6205DU",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[2].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "7205",
                    bearingTypes[1].Id, bearingTypes[1].Name,
                    25, 52, 15, brands[2].Id, 0.13m));

                // HRB 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205-2RS",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[3].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6205-Z",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[3].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[3].Id, 0.17m));

                // ZWZ 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[4].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6206",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    30, 62, 16, brands[4].Id, 0.15m));

                // LYC 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[5].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6310",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    50, 110, 27, brands[5].Id, 0.85m));

                await context.Bearings.AddRangeAsync(bearings);
                await context.SaveChangesAsync();

                // 补充技术参数和产地
                var bearingIndex = 0;

                // SKF 系列
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");

                // FAG 系列
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");

                // NSK 系列
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "ZZ", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");

                // HRB 系列
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "2RS", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Z", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");

                // ZWZ 系列
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");

                // LYC 系列
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");
                bearings[bearingIndex++].UpdateTechnicalSpecs("P0", "GCr15", "Open", "钢保持架");

                // 设置产地和类别
                bearingIndex = 0;
                bearings[bearingIndex++].SetOrigin("瑞典", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("瑞典", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("瑞典", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("瑞典", BearingCategory.Imported);

                bearings[bearingIndex++].SetOrigin("德国", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("德国", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("德国", BearingCategory.Imported);

                bearings[bearingIndex++].SetOrigin("日本", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("日本", BearingCategory.Imported);
                bearings[bearingIndex++].SetOrigin("日本", BearingCategory.Imported);

                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);

                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);

                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", BearingCategory.Domestic);

                await context.SaveChangesAsync();
            }

            #endregion

            // ============ 5. 替代品关系（仅开发环境） ============

            #region 替代品关系

            if (isDevelopment && bearings.Count >= 16)
            {
                var skf6205 = bearings[0];
                var fag6205 = bearings[4];
                var nsk6205 = bearings[7];
                var hrb6205_2rs = bearings[10];
                var zwz6205 = bearings[13];
                var lyc6205 = bearings[15];
                var skf6305 = bearings[2];

                var interchanges = new List<BearingInterchange>
                {
                    new(skf6205.Id, fag6205.Id, "exact", 100, "SKF官方互换表", "完全替代", true),
                    new(skf6205.Id, nsk6205.Id, "exact", 100, "SKF官方互换表", "完全替代", true),
                    new(skf6205.Id, hrb6205_2rs.Id, "exact", 95, "国标互换手册", "带密封圈", true),
                    new(skf6205.Id, zwz6205.Id, "exact", 90, "国标互换手册", "完全替代", true),
                    new(skf6205.Id, lyc6205.Id, "exact", 90, "国标互换手册", "完全替代", true),
                    new(skf6205.Id, skf6305.Id, "conditional", 60, "尺寸相近", "内径相同，外径和宽度更大", false)
                };

                await context.BearingInterchanges.AddRangeAsync(interchanges);
                await context.SaveChangesAsync();
            }
            #endregion

            // ============ 9. 系统配置（必须） ============

            #region 系统配置
            // 改动说明：配置默认值改为引用唯一的 ConfigDefaults 数组，与 EnsureConfigKeysAsync 共用，
            //           避免两处重复维护导致新增配置时只改一处而漂移
            var configs = ConfigDefaults
                .Select(d => new SystemConfig(d.Key, d.Value, d.Group, d.Description, d.ValueType, d.IsSystem))
                .ToList();

            await context.SystemConfigs.AddRangeAsync(configs);
            await context.SaveChangesAsync();

            #endregion
        }

        /// <summary>
        /// 系统配置默认值定义
        /// 职责：作为所有系统配置键的唯一权威来源，主种子与幂等补全共用同一份定义
        /// 改动说明：此前同样 14 项在两处各维护一份，且补全处的元组字段名写作 IsPublic
        ///           却传给 isSystem 参数，命名与语义错位，未来必然产生漂移
        /// </summary>
        private static readonly (string Key, string Value, string Group, string Description, string ValueType, bool IsSystem)[] ConfigDefaults =
        [
            // 站点设置（供移动端 /api/mobile/config 展示）
            ("SiteName", "OpenFindBearings", "Site", "网站名称", "string", true),
            ("SiteDescription", "轴承信息平台", "Site", "网站描述", "string", true),
            ("Site.BeiAn", "", "Site", "备案号", "string", true),
            ("Site.CustomerService", "", "Site", "客服联系方式", "string", true),

            // 价格显示
            ("Price.DefaultVisibility", "LoginRequired", "Price", "价格默认可见性", "string", true),
            ("Price.ShowNegotiableLabel", "true", "Price", "是否显示议价标签", "bool", true),
            ("Price.NumericForSorting", "true", "Price", "是否启用数值化价格", "bool", true),
            ("Price.ExtractPattern", @"¥(\d+(?:\.\d+)?)", "Price", "价格提取正则（需含首个捕获组）", "string", false),

            // 数据同步（限流 + 可信度阈值）
            ("RateLimit.Guest.RequestsPerMinute", "30", "Sync", "游客每分钟请求数", "int", true),
            ("RateLimit.User.RequestsPerMinute", "60", "Sync", "用户每分钟请求数", "int", true),
            ("RateLimit.Premium.RequestsPerMinute", "120", "Sync", "付费用户每分钟请求数", "int", true),
            ("Reliability.AutoSyncThreshold", "85", "Sync", "自动同步阈值（可信度≥此值直接入库）", "int", false),
            ("Reliability.ReviewThreshold", "60", "Sync", "人工审核阈值（可信度≥此值进入待审核）", "int", false),
            ("Reliability.DefaultSourceScore", "80", "Sync", "来源默认基础分", "int", false),
        ];

        /// <summary>
        /// 幂等补全系统配置键，避免已有库（SystemConfigs 非空、跳过主种子）缺少配置键导致不可编辑
        /// </summary>
        private static async Task EnsureConfigKeysAsync(ApplicationDbContext context)
        {
            foreach (var d in ConfigDefaults)
            {
                if (!await context.SystemConfigs.AnyAsync(c => c.Key == d.Key))
                {
                    context.SystemConfigs.Add(new SystemConfig(d.Key, d.Value, d.Group, d.Description, d.ValueType, d.IsSystem));
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
