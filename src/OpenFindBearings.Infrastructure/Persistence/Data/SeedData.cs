using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(ApplicationDbContext context, bool isDevelopment)
        {
            if (await context.SystemConfigs.AnyAsync())
            {
                return;
            }

            // ============ 1. 基础字典数据 ============

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

            // ============ 2. 角色和权限 ============

            // 创建权限
            var permissions = new List<Permission>
            {
                // 产品管理
                new("product.view", "查看产品"),
                new("product.create", "创建产品"),
                new("product.edit", "编辑产品"),
                new("product.delete", "删除产品"),
                
                // 商家管理
                new("merchant.view", "查看商家"),
                new("merchant.verify", "认证商家"),
                new("merchant.manage", "管理商家"),
                
                // 纠错管理
                new("correction.submit", "提交纠错"),
                new("correction.review", "审核纠错"),
                
                // 收藏关注
                new("favorite.bearing", "收藏轴承"),
                new("favorite.merchant", "关注商家"),
                
                // 用户管理
                new("user.manage", "管理用户"),
                new("role.manage", "管理角色"),
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            // 创建角色
            var roles = new List<Role>
            {
                new("GlobalAdmin", "平台超级管理员"),
                new("MerchantAdmin", "商家管理员"),
                new("MerchantStaff", "商家员工"),
                new("Customer", "普通用户")
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            // 分配权限给角色
            var globalAdmin = roles.First(r => r.Name == "GlobalAdmin");
            var merchantAdmin = roles.First(r => r.Name == "MerchantAdmin");
            var merchantStaff = roles.First(r => r.Name == "MerchantStaff");
            var customer = roles.First(r => r.Name == "Customer");

            var rolePermissions = new List<RolePermission>
            {
                // GlobalAdmin 拥有所有权限
                new(globalAdmin.Id, permissions.First(p => p.Name == "product.view").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "product.create").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "product.edit").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "product.delete").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "merchant.view").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "merchant.verify").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "merchant.manage").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "correction.review").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "favorite.bearing").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "favorite.merchant").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "user.manage").Id),
                new(globalAdmin.Id, permissions.First(p => p.Name == "role.manage").Id),

                // MerchantAdmin 拥有商家管理权限
                new(merchantAdmin.Id, permissions.First(p => p.Name == "product.view").Id),
                new(merchantAdmin.Id, permissions.First(p => p.Name == "product.create").Id),
                new(merchantAdmin.Id, permissions.First(p => p.Name == "product.edit").Id),
                new(merchantAdmin.Id, permissions.First(p => p.Name == "merchant.view").Id),

                // MerchantStaff 拥有查看权限
                new(merchantStaff.Id, permissions.First(p => p.Name == "product.view").Id),
                new(merchantStaff.Id, permissions.First(p => p.Name == "merchant.view").Id),

                // Customer 拥有基本权限
                new(customer.Id, permissions.First(p => p.Name == "product.view").Id),
                new(customer.Id, permissions.First(p => p.Name == "correction.submit").Id),
                new(customer.Id, permissions.First(p => p.Name == "favorite.bearing").Id),
                new(customer.Id, permissions.First(p => p.Name == "favorite.merchant").Id),
            };

            await context.RolePermissions.AddRangeAsync(rolePermissions);
            await context.SaveChangesAsync();

            // ============ 3. 用户数据 ============

            // 创建测试用户
            var users = new List<User>
            {
                new("auth-admin-001", UserType.Admin, RegistrationSource.Admin, null, "系统管理员")
            };

            if (isDevelopment)
            {
                users.AddRange(
                [
                    new("auth-merchant-001", UserType.MerchantStaff, RegistrationSource.Mobile, null, "张经理"),
                    new("auth-merchant-002", UserType.MerchantStaff, RegistrationSource.Mobile, null, "李经理"),
                    new("auth-customer-001", UserType.Individual, RegistrationSource.Mobile, null, "王先生"),
                    new("auth-customer-002", UserType.Individual, RegistrationSource.Mobile, null, "赵女士"),
                ]);
            }

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // 分配用户角色
            var userRoles = new List<UserRole>
            {
                new(users[0].Id, globalAdmin.Id)
            };

            if (isDevelopment)
            {
                userRoles.AddRange(
                [
                    new(users[1].Id, merchantStaff.Id),
                    new(users[2].Id, merchantStaff.Id),
                    new(users[3].Id, customer.Id),
                    new(users[4].Id, customer.Id),
                ]);
            }

            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();

            if (isDevelopment && bearingTypes.Any() && brands.Any())
            {
                // ============ 4. 轴承产品数据 ============

                var bearings = new List<Bearing>();

                // SKF 品牌的产品
                bearings.Add(CreateBearing(
                    currentCode: "6205",
                    name: "SKF 深沟球轴承 6205",
                    bearingTypeId: bearingTypes[0].Id,
                    bearingType: bearingTypes[0].Name,
                    innerDiameter: 25, outerDiameter: 52, width: 15,
                    brandId: brands[0].Id,
                    weight: 0.12m));

                bearings.Add(CreateBearing(
                    "6206", "SKF 深沟球轴承 6206",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    30, 62, 16, brands[0].Id, 0.15m));

                bearings.Add(CreateBearing(
                    "6305", "SKF 深沟球轴承 6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[0].Id, 0.17m));

                bearings.Add(CreateBearing(
                    "6310", "SKF 深沟球轴承 6310",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    50, 110, 27, brands[0].Id, 0.85m));

                // FAG 品牌的产品
                bearings.Add(CreateBearing(
                    "6205", "FAG 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[1].Id, 0.12m));

                bearings.Add(CreateBearing(
                    "6305", "FAG 深沟球轴承 6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[1].Id, 0.17m));

                bearings.Add(CreateBearing(
                    "7205-B", "FAG 角接触球轴承 7205-B",
                    bearingTypes[1].Id, bearingTypes[1].Name,
                    25, 52, 15, brands[1].Id, 0.13m));

                // NSK 品牌的产品
                bearings.Add(CreateBearing(
                    "6205", "NSK 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[2].Id, 0.12m));

                bearings.Add(CreateBearing(
                    "6205DU", "NSK 深沟球轴承 6205DU",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[2].Id, 0.12m));

                bearings.Add(CreateBearing(
                    "7205", "NSK 角接触球轴承 7205",
                    bearingTypes[1].Id, bearingTypes[1].Name,
                    25, 52, 15, brands[2].Id, 0.13m));

                // HRB 品牌的产品
                bearings.Add(CreateBearing(
                    "6205-2RS", "HRB 深沟球轴承 6205-2RS",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[3].Id, 0.12m));

                bearings.Add(CreateBearing(
                    "6205-Z", "HRB 深沟球轴承 6205-Z",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[3].Id, 0.12m));

                bearings.Add(CreateBearing(
                    "6305", "HRB 深沟球轴承 6305",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 62, 17, brands[3].Id, 0.17m));

                // ZWZ 品牌的产品
                bearings.Add(CreateBearing(
                    "6205", "ZWZ 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[4].Id, 0.12m));

                bearings.Add(CreateBearing(
                    "6206", "ZWZ 深沟球轴承 6206",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    30, 62, 16, brands[4].Id, 0.15m));

                // LYC 品牌的产品
                bearings.Add(CreateBearing(
                    "6205", "LYC 深沟球轴承 6205",
                    bearingTypes[0].Id, bearingTypes[0].Name,
                    25, 52, 15, brands[5].Id, 0.12m));

                bearings.Add(CreateBearing(
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

                // ============ 5. 替代品关系 ============

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

                // ============ 6. 商家数据 ============

                var merchants = new List<Merchant>
                {
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
                };

                await context.Merchants.AddRangeAsync(merchants);
                await context.SaveChangesAsync();

                // 将商家员工关联到商家
                users[1].AssignToMerchant(merchants[0].Id);
                users[2].AssignToMerchant(merchants[1].Id);

                await context.SaveChangesAsync();

                // ============ 7. 商家-产品关联 ============

                var merchantBearings = new List<MerchantBearing>
                {
                    // 上海轴承公司
                    new(merchants[0].Id, skf6205.Id, "¥55-60", "现货"),
                    new(merchants[0].Id, bearings[1].Id, "¥65-70", "现货"),
                    new(merchants[0].Id, bearings[2].Id, "¥85-95", "现货"),
                    new(merchants[0].Id, fag6205.Id, "¥58-65", "期货"),
                
                    // 广州进口轴承
                    new(merchants[1].Id, skf6205.Id, "¥58-65", "现货"),
                    new(merchants[1].Id, nsk6205.Id, "¥56-62", "现货"),
                    new(merchants[1].Id, bearings[2].Id, "¥80-90", "需预订"),
                
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

                // ============ 8. 用户收藏数据 ============

                var userFavorites = new List<UserBearingFavorite>
                {
                    new(users[3].Id, skf6205.Id),
                    new(users[3].Id, nsk6205.Id),
                    new(users[4].Id, fag6205.Id),
                    new(users[4].Id, hrb6205_2rs.Id),
                };

                await context.UserFavorites.AddRangeAsync(userFavorites);
                await context.SaveChangesAsync();

                // ============ 9. 用户关注数据 ============

                var userFollows = new List<UserMerchantFollow>
                {
                    new(users[3].Id, merchants[0].Id),
                    new(users[3].Id, merchants[1].Id),
                    new(users[4].Id, merchants[2].Id),
                };

                await context.UserFollows.AddRangeAsync(userFollows);
                await context.SaveChangesAsync();

                // ============ 10. 浏览历史数据 ============

                await context.UserBearingHistories.AddRangeAsync([
                    new(users[3].Id, skf6205.Id),
                    new(users[3].Id, bearings[2].Id),
                    new(users[3].Id, fag6205.Id),
                    new(users[4].Id, nsk6205.Id),
                    new(users[4].Id, hrb6205_2rs.Id),
                ]);

                await context.UserMerchantHistories.AddRangeAsync([
                    new(users[3].Id, merchants[0].Id),
                    new(users[3].Id, merchants[1].Id),
                ]);

                await context.SaveChangesAsync();
            }

            // ============ 11. 系统配置 ============
            if (!await context.SystemConfigs.AnyAsync())
            {
                var configs = new List<SystemConfig>
                {
                    // 基础配置
                    new("SiteName", "OpenFindBearings", "General", "网站名称", "string", true),
                    new("SiteDescription", "轴承信息平台", "General", "网站描述", "string", true),
                    new("ItemsPerPage", "20", "Pagination", "默认每页条数", "int", true),
                    new("EnableRegistration", "true", "User", "是否允许注册", "bool", true),
                    new("RequireEmailVerification", "false", "User", "是否需要邮箱验证", "bool", true),
                    new("DefaultUserRole", "Customer", "User", "默认用户角色", "string", true),
                    
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
            }
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 创建轴承
        /// </summary>
        private static Bearing CreateBearing(
            string currentCode,
            string name,
            Guid bearingTypeId,
            string bearingType,
            decimal innerDiameter,
            decimal outerDiameter,
            decimal width,
            Guid brandId,
            decimal? weight = null)
        {
            var dimensions = new Dimensions(innerDiameter, outerDiameter, width);
            return new Bearing(
                currentCode: currentCode,
                name: name,
                bearingTypeId: bearingTypeId,
                bearingType: bearingType,
                dimensions: dimensions,
                brandId: brandId,
                performance: null,
                weight: weight);
        }
    }
}
