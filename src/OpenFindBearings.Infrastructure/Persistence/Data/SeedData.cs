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
            isDevelopment = true; // TODO: 正式发布的时候取消，目前测试期间isDevelopment始终为true

            try
            {
                await using var context = provider.GetRequiredService<ApplicationDbContext>();
                await using var scope = provider.CreateAsyncScope();

                if (isDevelopment)
                {
                    await context.Database.EnsureDeletedAsync();  // 测试期间每次删除，节省每次手动删库的操作
                }

                await context.Database.MigrateAsync();

                await ExecuteAsync(context, logger, isDevelopment);

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
                new("product.view", "查看产品"),
                new("product.create", "创建产品"),
                new("product.edit", "编辑产品"),
                new("product.delete", "删除产品"),
                new("merchant.view", "查看商家"),
                new("merchant.verify", "认证商家"),
                new("merchant.manage", "管理商家"),
                new("correction.submit", "提交纠错"),
                new("correction.review", "审核纠错"),
                new("favorite.bearing", "收藏轴承"),
                new("favorite.merchant", "关注商家"),
                new("user.manage", "管理用户"),
                new("role.manage", "管理角色"),
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
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "product.view").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "product.create").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "product.edit").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "merchant.view").Id),
                new(merchantAdminRole.Id, permissions.First(p => p.Name == "merchant.manage").Id),
            ]);

            // MerchantStaff 拥有查看权限
            rolePermissions.AddRange([
                new(merchantStaffRole.Id, permissions.First(p => p.Name == "product.view").Id),
                new(merchantStaffRole.Id, permissions.First(p => p.Name == "merchant.view").Id),
            ]);

            // Individual 拥有基本权限
            rolePermissions.AddRange([
                new(individualRole.Id, permissions.First(p => p.Name == "product.view").Id),
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

            // 3.1 默认超级管理员（无论开发/生产都需要）
            var adminUser = new User(
                authUserId: "admin-default",
                registrationSource: RegistrationSource.Admin,
                registerIp: null,
                nickname: "系统管理员"
            );
            users.Add(adminUser);
            userRoles.Add(new UserRole(adminUser.Id, adminRole.Id));

            // 3.2 开发环境额外测试用户
            if (isDevelopment)
            {
                var merchant1 = new User("auth-merchant-001", RegistrationSource.Mobile, null, "张经理");
                var merchant2 = new User("auth-merchant-002", RegistrationSource.Mobile, null, "李经理");
                var customer1 = new User("auth-customer-001", RegistrationSource.Mobile, null, "王先生");
                var customer2 = new User("auth-customer-002", RegistrationSource.Mobile, null, "赵女士");

                users.AddRange([merchant1, merchant2, customer1, customer2]);

                userRoles.AddRange([
                    new UserRole(merchant1.Id, merchantStaffRole.Id),
                    new UserRole(merchant2.Id, merchantStaffRole.Id),
                    new UserRole(customer1.Id, individualRole.Id),
                    new UserRole(customer2.Id, individualRole.Id),
                ]);
            }

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();
            #endregion

            // ============ 4. 轴承产品数据（仅开发环境） ============

            #region 轴承产品数据

            var bearings = new List<Bearing>();

            if (isDevelopment && bearingTypes.Any() && brands.Any())
            {
                // SKF 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    currentCode: "6205",
                    name: "SKF 深沟球轴承 6205",
                    bearingTypeId: bearingTypes[0].Id,
                    bearingType: bearingTypes[0].Name,
                    innerDiameter: 25, outerDiameter: 52, width: 15,
                    brandId: brands[0].Id,
                    weight: 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6206", "SKF 深沟球轴承 6206",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    30, 62, 16, brands[0].Id, 0.15m));

                bearings.Add(Bearing.CreateBearing(
                    "6305", "SKF 深沟球轴承 6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[0].Id, 0.17m));

                bearings.Add(Bearing.CreateBearing(
                    "6310", "SKF 深沟球轴承 6310",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    50, 110, 27, brands[0].Id, 0.85m));

                // FAG 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205", "FAG 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[1].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6305", "FAG 深沟球轴承 6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[1].Id, 0.17m));

                bearings.Add(Bearing.CreateBearing(
                    "7205-B", "FAG 角接触球轴承 7205-B",
                    bearingTypes[1].Id, bearingTypes[1].Name,
                    25, 52, 15, brands[1].Id, 0.13m));

                // NSK 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205", "NSK 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[2].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6205DU", "NSK 深沟球轴承 6205DU",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[2].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "7205", "NSK 角接触球轴承 7205",
                    bearingTypes[1].Id, bearingTypes[1].Name,
                    25, 52, 15, brands[2].Id, 0.13m));

                // HRB 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205-2RS", "HRB 深沟球轴承 6205-2RS",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[3].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6205-Z", "HRB 深沟球轴承 6205-Z",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[3].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6305", "HRB 深沟球轴承 6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[3].Id, 0.17m));

                // ZWZ 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205", "ZWZ 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[4].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6206", "ZWZ 深沟球轴承 6206",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    30, 62, 16, brands[4].Id, 0.15m));

                // LYC 品牌的产品
                bearings.Add(Bearing.CreateBearing(
                    "6205", "LYC 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[5].Id, 0.12m));

                bearings.Add(Bearing.CreateBearing(
                    "6310", "LYC 深沟球轴承 6310",
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

            // ============ 6. 商家数据（仅开发环境） ============

            #region 商家数据
            var merchants = new List<Merchant>();
            if (isDevelopment)
            {
                merchants.AddRange(
                [
                    new("上海轴承公司", MerchantType.Distributor,
                        new ContactInfo("张经理", "021-12345678", "13912345678",
                            "sh@bearing.com", "上海市浦东新区")),
                    new("广州进口轴承", MerchantType.AuthorizedDealer,
                        new ContactInfo("李经理", "020-87654321", "13887654321",
                            "gz@bearing.com", "广州市天河区")),
                    new("天津贸易商行", MerchantType.Trader,
                        new ContactInfo("王经理", "022-11223344", "13711223344",
                            "tj@bearing.com", "天津市滨海新区")),
                    new("沈阳轴承批发", MerchantType.Distributor,
                        new ContactInfo("刘经理", "024-12345678", "13912345679",
                            "sy@bearing.com", "沈阳市铁西区")),
                    new("西安进口轴承", MerchantType.AuthorizedDealer,
                        new ContactInfo("陈经理", "029-87654321", "13887654322",
                            "xa@bearing.com", "西安市高新区"))
                ]);

                await context.Merchants.AddRangeAsync(merchants);
                await context.SaveChangesAsync();

                // 将商家员工关联到商家（确保索引存在）
                if (users.Count >= 3 && merchants.Count >= 2)
                {
                    var merchant1 = merchants[0];
                    var merchant2 = merchants[1];
                    var merchantStaff1 = users.FirstOrDefault(u => u.Nickname == "张经理");
                    var merchantStaff2 = users.FirstOrDefault(u => u.Nickname == "李经理");

                    if (merchantStaff1 != null) merchantStaff1.AssignToMerchant(merchant1.Id);
                    if (merchantStaff2 != null) merchantStaff2.AssignToMerchant(merchant2.Id);

                    await context.SaveChangesAsync();
                }
            }
            #endregion

            // ============ 7. 商家-产品关联（仅开发环境） ============

            #region 商家-产品关联

            if (isDevelopment && merchants.Count >= 5 && bearings.Count >= 15)
            {
                var skf6205 = bearings[0];
                var fag6205 = bearings[4];
                var nsk6205 = bearings[7];
                var hrb6205_2rs = bearings[10];
                var zwz6205 = bearings[13];
                var lyc6205 = bearings[15];
                var bearing6206 = bearings[1];
                var bearing6305 = bearings[2];
                var bearing6205DU = bearings[8];
                var bearing7205 = bearings[9];

                var merchantBearings = new List<MerchantBearing>
                {
                    // 上海轴承公司
                    new(merchants[0].Id, skf6205.Id, "¥55-60", "现货"),
                    new(merchants[0].Id, bearing6206.Id, "¥65-70", "现货"),
                    new(merchants[0].Id, bearing6305.Id, "¥85-95", "现货"),
                    new(merchants[0].Id, fag6205.Id, "¥58-65", "期货"),
                
                    // 广州进口轴承
                    new(merchants[1].Id, skf6205.Id, "¥58-65", "现货"),
                    new(merchants[1].Id, nsk6205.Id, "¥56-62", "现货"),
                    new(merchants[1].Id, bearing6305.Id, "¥80-90", "需预订"),
                
                    // 天津贸易商行
                    new(merchants[2].Id, skf6205.Id, "电议", "期货"),
                    new(merchants[2].Id, hrb6205_2rs.Id, "¥45-52", "现货"),
                
                    // 沈阳轴承批发
                    new(merchants[3].Id, zwz6205.Id, "¥42-48", "现货"),
                    new(merchants[3].Id, bearings[14].Id, "¥50-58", "现货"),
                    new(merchants[3].Id, lyc6205.Id, "¥44-50", "现货"),
                
                    // 西安进口轴承
                    new(merchants[4].Id, fag6205.Id, "电议", "期货"),
                    new(merchants[4].Id, nsk6205.Id, "¥58-65", "期货"),
                    new(merchants[4].Id, bearings[6].Id, "¥75-85", "现货")
                };

                await context.MerchantBearings.AddRangeAsync(merchantBearings);
                await context.SaveChangesAsync();

                // 设置推荐产品
                merchantBearings[0].SetFeatured(true);
                merchantBearings[3].SetFeatured(true);
                merchantBearings[4].SetFeatured(true);

                await context.SaveChangesAsync();
            }

            #endregion

            // ============ 8. 用户个人数据（仅开发环境） ============

            #region 用户个人数据

            if (isDevelopment && users.Count >= 5 && merchants.Count >= 3 && bearings.Count >= 10)
            {
                var customer1 = users.FirstOrDefault(u => u.Nickname == "王先生");
                var customer2 = users.FirstOrDefault(u => u.Nickname == "赵女士");

                if (customer1 != null && customer2 != null)
                {
                    var skf6205 = bearings[0];
                    var fag6205 = bearings[4];
                    var nsk6205 = bearings[7];
                    var hrb6205_2rs = bearings[10];
                    var bearing6305 = bearings[2];

                    // 收藏数据
                    var userFavorites = new List<UserBearingFavorite>
                    {
                        new(customer1.Id, skf6205.Id),
                        new(customer1.Id, nsk6205.Id),
                        new(customer2.Id, fag6205.Id),
                        new(customer2.Id, hrb6205_2rs.Id),
                    };

                    await context.UserFavorites.AddRangeAsync(userFavorites);
                    await context.SaveChangesAsync();

                    // 关注数据
                    var userFollows = new List<UserMerchantFollow>
                    {
                        new(customer1.Id, merchants[0].Id),
                        new(customer1.Id, merchants[1].Id),
                        new(customer2.Id, merchants[2].Id),
                    };

                    await context.UserFollows.AddRangeAsync(userFollows);
                    await context.SaveChangesAsync();

                    // 浏览历史数据
                    await context.UserBearingHistories.AddRangeAsync([
                        new(customer1.Id, skf6205.Id),
                        new(customer1.Id, bearing6305.Id),
                        new(customer1.Id, fag6205.Id),
                        new(customer2.Id, nsk6205.Id),
                        new(customer2.Id, hrb6205_2rs.Id),
                    ]);

                    await context.UserMerchantHistories.AddRangeAsync([
                        new(customer1.Id, merchants[0].Id),
                        new(customer1.Id, merchants[1].Id),
                    ]);

                    await context.SaveChangesAsync();
                }
            }

            #endregion

            // ============ 9. 系统配置（必须） ============

            #region 系统配置
            var configs = new List<SystemConfig>
            {
                // 基础配置
                new("SiteName", "OpenFindBearings", "General", "网站名称", "string", true),
                new("SiteDescription", "轴承信息平台", "General", "网站描述", "string", true),
                new("ItemsPerPage", "20", "Pagination", "默认每页条数", "int", true),
                new("EnableRegistration", "true", "User", "是否允许注册", "bool", true),
                new("RequireEmailVerification", "false", "User", "是否需要邮箱验证", "bool", true),
                // ✅ 修正：角色名改为 Individual
                new("DefaultUserRole", "Individual", "User", "默认用户角色", "string", true),
                    
                // 价格配置
                new("Price.DefaultVisibility", "LoginRequired", "Price", "价格默认可见性", "string", true),
                new("Price.ShowNegotiableLabel", "true", "Price", "是否显示议价标签", "bool", true),
                new("Price.NumericForSorting", "true", "Price", "是否启用数值化价格", "bool", true),
                    
                // 缓存配置
                new("Cache.DefaultExpirationMinutes", "60", "Performance", "缓存默认过期时间", "int", true),
                new("Cache.EnableRedis", "false", "Performance", "是否启用Redis缓存", "bool", true),
                    
                // 限流配置
                new("RateLimit.Guest.RequestsPerMinute", "30", "RateLimit", "游客每分钟请求数", "int", true),
                new("RateLimit.User.RequestsPerMinute", "60", "RateLimit", "用户每分钟请求数", "int", true),
                new("RateLimit.Premium.RequestsPerMinute", "120", "RateLimit", "付费用户每分钟请求数", "int", true),
            };

            await context.SystemConfigs.AddRangeAsync(configs);
            await context.SaveChangesAsync();

            #endregion
        }
    }
}
