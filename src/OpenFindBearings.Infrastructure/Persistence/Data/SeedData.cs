using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 检查是否已有数据
            if (await context.SystemConfigs.AnyAsync())
                return;

            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDev = string.IsNullOrWhiteSpace(envName) || envName == "Development"; // 默认视为开发环境

            // ============ 1. 基础字典数据 ============

            var brands = new List<Brand>();
            if (isDev)
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
            }

            var bearingTypes = new List<BearingType>();
            if (isDev)
            {
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
                new("auth-admin-001", UserType.Admin, "系统管理员")
            };

            if (isDev)
            {
                users.AddRange(
                [
                    new("auth-merchant-001", UserType.MerchantStaff, "张经理"),
                    new("auth-merchant-002", UserType.MerchantStaff, "李经理"),
                    new("auth-customer-001", UserType.Individual, "王先生"),
                    new("auth-customer-002", UserType.Individual, "赵女士"),
                ]);
            }

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // 分配用户角色
            var userRoles = new List<UserRole>
            {
                new(users[0].Id, globalAdmin.Id)
            };

            if (!isDev)
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

            if (isDev)
            {
                // ============ 4. 轴承产品数据 ============

                var bearings = new List<Bearing>
                {
                    // SKF 品牌的产品
                    CreateBearing("6205", "SKF 深沟球轴承 6205", 25, 52, 15,
                    bearingTypes[0], brands[0], weight: 0.12m),
                    CreateBearing("6206", "SKF 深沟球轴承 6206", 30, 62, 16,
                    bearingTypes[0], brands[0], weight: 0.15m),
                    CreateBearing("6305", "SKF 深沟球轴承 6305", 25, 62, 17,
                    bearingTypes[0], brands[0], weight: 0.17m),
                    CreateBearing("6310", "SKF 深沟球轴承 6310", 50, 110, 27,
                    bearingTypes[0], brands[0], weight: 0.85m),

                    // FAG 品牌的产品
                    CreateBearing("6205", "FAG 深沟球轴承 6205", 25, 52, 15,
                    bearingTypes[0], brands[1], weight: 0.12m),
                    CreateBearing("6305", "FAG 深沟球轴承 6305", 25, 62, 17,
                    bearingTypes[0], brands[1], weight: 0.17m),
                    CreateBearing("7205-B", "FAG 角接触球轴承 7205-B", 25, 52, 15,
                    bearingTypes[1], brands[1], weight: 0.13m),

                    // NSK 品牌的产品
                    CreateBearing("6205", "NSK 深沟球轴承 6205", 25, 52, 15,
                    bearingTypes[0], brands[2], weight: 0.12m),
                    CreateBearing("6205DU", "NSK 深沟球轴承 6205DU", 25, 52, 15,
                    bearingTypes[0], brands[2], weight: 0.12m),
                    CreateBearing("7205", "NSK 角接触球轴承 7205", 25, 52, 15,
                    bearingTypes[1], brands[2], weight: 0.13m),

                    // HRB 品牌的产品
                    CreateBearing("6205-2RS", "HRB 深沟球轴承 6205-2RS", 25, 52, 15,
                    bearingTypes[0], brands[3], weight: 0.12m),
                    CreateBearing("6205-Z", "HRB 深沟球轴承 6205-Z", 25, 52, 15,
                    bearingTypes[0], brands[3], weight: 0.12m),
                    CreateBearing("6305", "HRB 深沟球轴承 6305", 25, 62, 17,
                    bearingTypes[0], brands[3], weight: 0.17m),

                    // ZWZ 品牌的产品
                    CreateBearing("6205", "ZWZ 深沟球轴承 6205", 25, 52, 15,
                    bearingTypes[0], brands[4], weight: 0.12m),
                    CreateBearing("6206", "ZWZ 深沟球轴承 6206", 30, 62, 16,
                    bearingTypes[0], brands[4], weight: 0.15m),

                    // LYC 品牌的产品
                    CreateBearing("6205", "LYC 深沟球轴承 6205", 25, 52, 15,
                    bearingTypes[0], brands[5], weight: 0.12m),
                    CreateBearing("6310", "LYC 深沟球轴承 6310", 50, 110, 27,
                    bearingTypes[0], brands[5], weight: 0.85m)
                };

                await context.Bearings.AddRangeAsync(bearings);
                await context.SaveChangesAsync();

                // 补充技术参数
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

                // 为现有轴承设置产地和类别

                // SKF 系列（瑞典，进口）
                bearingIndex = 0;
                bearings[bearingIndex++].SetOrigin("瑞典", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("瑞典", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("瑞典", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("瑞典", ProductCategory.Imported);

                // FAG 系列（德国，进口）
                bearings[bearingIndex++].SetOrigin("德国", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("德国", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("德国", ProductCategory.Imported);

                // NSK 系列（日本，进口）
                bearings[bearingIndex++].SetOrigin("日本", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("日本", ProductCategory.Imported);
                bearings[bearingIndex++].SetOrigin("日本", ProductCategory.Imported);

                // HRB 系列（中国，国产）
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);

                // ZWZ 系列（中国，国产）
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);

                // LYC 系列（中国，国产）
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);
                bearings[bearingIndex++].SetOrigin("中国", ProductCategory.Domestic);

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
                    new(skf6205.Id, fag6205.Id, "exact", 100, "SKF官方互换表", remarks: "完全替代", isBidirectional: true),
                    new(skf6205.Id, nsk6205.Id, "exact", 100, "SKF官方互换表", remarks: "完全替代", isBidirectional: true),
                    new(skf6205.Id, hrb6205_2rs.Id, "exact", 95, "国标互换手册", remarks: "带密封圈", isBidirectional: true),
                    new(skf6205.Id, zwz6205.Id, "exact", 90, "国标互换手册", remarks: "完全替代", isBidirectional: true),
                    new(skf6205.Id, lyc6205.Id, "exact", 90, "国标互换手册", remarks: "完全替代", isBidirectional: true),
                    new(skf6205.Id, skf6305.Id, "conditional", 60, "尺寸相近", remarks: "内径相同，外径和宽度更大", isBidirectional: false)
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
                users[1].AssignToMerchant(merchants[0].Id); // 张经理 -> 上海轴承公司
                users[2].AssignToMerchant(merchants[1].Id); // 李经理 -> 广州进口轴承

                await context.SaveChangesAsync();

                // ============ 7. 商家-产品关联（带价格可见性） ============

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

                // 设置价格可见性
                // 注意：需要为 MerchantBearing 实体添加 SetPriceVisibility 方法
                // 这里假设已经有了这个方法
                foreach (var mb in merchantBearings)
                {
                    // 设置数值化价格（用于排序）
                    // 从价格描述中提取数值
                    if (!string.IsNullOrWhiteSpace(mb.PriceDescription))
                    {
                        if (mb.PriceDescription.Contains("¥"))
                        {
                            // 简单提取第一个数字作为示例
                            // 实际项目中可能需要更复杂的解析逻辑
                            // mb.SetNumericPrice(ExtractNumericPrice(mb.PriceDescription));
                        }
                    }

                    // 设置价格可见性
                    // 电议的价格设为登录可见
                    if (mb.PriceDescription?.Contains("电议") == true)
                    {
                        // mb.SetPriceVisibility(PriceVisibility.LoginRequired);
                    }
                    else
                    {
                        // 普通价格设为公开可见
                        // mb.SetPriceVisibility(PriceVisibility.Public);
                    }
                }

                // 设置一些推荐产品
                merchantBearings[0].SetFeatured(true);
                merchantBearings[3].SetFeatured(true);
                merchantBearings[4].SetFeatured(true);

                await context.SaveChangesAsync();

                // ============ 8. 用户收藏数据 ============

                var userFavorites = new List<UserBearingFavorite>
                {
                    // 王先生收藏 SKF6205 和 NSK6205
                    new(users[3].Id, skf6205.Id),
                    new(users[3].Id, nsk6205.Id),
                
                    // 赵女士收藏 FAG6205 和 HRB6205-2RS
                    new(users[4].Id, fag6205.Id),
                    new(users[4].Id, hrb6205_2rs.Id),
                };

                await context.UserFavorites.AddRangeAsync(userFavorites);
                await context.SaveChangesAsync();

                // ============ 9. 用户关注数据 ============

                var userFollows = new List<UserMerchantFollow>
                {
                    // 王先生关注上海轴承公司和广州进口轴承
                    new(users[3].Id, merchants[0].Id),
                    new(users[3].Id, merchants[1].Id),
                
                    // 赵女士关注天津贸易商行
                    new(users[4].Id, merchants[2].Id),
                };

                await context.UserFollows.AddRangeAsync(userFollows);
                await context.SaveChangesAsync();

                // ============ 10. 浏览历史数据 ============

                // 王先生的浏览历史
                await context.UserBearingHistories.AddAsync(new UserBearingHistory(users[3].Id, skf6205.Id));
                await context.UserBearingHistories.AddAsync(new UserBearingHistory(users[3].Id, bearings[2].Id));
                await context.UserBearingHistories.AddAsync(new UserBearingHistory(users[3].Id, fag6205.Id));

                // 赵女士的浏览历史
                await context.UserBearingHistories.AddAsync(new UserBearingHistory(users[4].Id, nsk6205.Id));
                await context.UserBearingHistories.AddAsync(new UserBearingHistory(users[4].Id, hrb6205_2rs.Id));

                await context.UserMerchantHistories.AddAsync(new UserMerchantHistory(users[3].Id, merchants[0].Id));
                await context.UserMerchantHistories.AddAsync(new UserMerchantHistory(users[3].Id, merchants[1].Id));

                await context.SaveChangesAsync();
            }

            // ============ 11. 系统配置（包含价格配置） ============
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
                    
                    // ============ 价格配置 ============
                    new("Price.DefaultVisibility", "LoginRequired", "Price",
                        "价格默认可见性: Public=所有人可见, LoginRequired=仅登录用户可见", "string", true),
                    new("Price.ShowNegotiableLabel", "true", "Price",
                        "是否显示'电议'、'面议'等议价标签", "bool", true),
                    new("Price.NumericForSorting", "true", "Price",
                        "是否启用数值化价格用于排序和筛选", "bool", true),
                    new("Price.ExtractPattern", @"¥(\d+(?:\.\d+)?)", "Price",
                        "从价格描述中提取数值的正则表达式", "string", true),
                    
                    // ============ 缓存配置 ============
                    new("Cache.DefaultExpirationMinutes", "60", "Performance",
                        "缓存默认过期时间（分钟）", "int", true),
                    new("Cache.EnableRedis", "false", "Performance",
                        "是否启用Redis缓存", "bool", true),
                    new("Cache.MemorySizeLimit", "100", "Performance",
                        "内存缓存大小限制（MB）", "int", true),
                    
                    // ============ 移动端配置 ============
                    new("Mobile.AppVersion", "1.0.0", "Mobile", "App最新版本号", "string", true),
                    new("Mobile.MinVersion", "1.0.0", "Mobile", "最低支持版本", "string", true),
                    new("Mobile.ForceUpdate", "false", "Mobile", "是否强制更新", "bool", true),
                    new("Mobile.DownloadUrl", "https://example.com/app", "Mobile", "App下载地址", "string", true),
                    new("Mobile.WxAppId", "your_wx_app_id", "WeChat", "微信小程序AppId", "string", false),
                    new("Mobile.WxSecret", "your_wx_secret", "WeChat", "微信小程序Secret", "string", false),
                    new("Mobile.WxApiUrl", "https://api.weixin.qq.com", "WeChat", "微信API地址", "string", true),
                    
                    // ============ 图片配置 ============
                    new("Image.MaxSize", "5242880", "Image", "图片最大大小(5MB)", "int", true),
                    new("Image.AllowedTypes", "jpg,jpeg,png,webp", "Image", "允许的图片格式", "string", true),
                    new("Image.CompressQuality", "80", "Image", "图片压缩质量(1-100)", "int", true),
                    
                    // ============ 分页配置 ============
                    new("Mobile.PageSize", "10", "Mobile", "移动端默认每页条数", "int", true),
                    new("Mobile.MaxPageSize", "50", "Mobile", "移动端最大每页条数", "int", true),
                    
                    // ============ 搜索配置 ============
                    new("Search.DefaultSortBy", "partnumber", "Search", "默认排序字段", "string", true),
                    new("Search.DefaultSortOrder", "asc", "Search", "默认排序方向", "string", true),
                    new("Search.EnableFuzzySearch", "true", "Search", "是否启用模糊搜索", "bool", true),
                };

                await context.SystemConfigs.AddRangeAsync(configs);
                await context.SaveChangesAsync();
            }

            // ============ 12. 辅助方法 ============

            // 创建轴承
            static Bearing CreateBearing(
                string partNumber,
                string name,
                decimal innerDia, decimal outerDia, decimal width,
                BearingType type, Brand brand,
                PerformanceParams? performance = null,
                decimal? weight = null)
            {
                var dimensions = new Dimensions(innerDia, outerDia, width);
                return new Bearing(
                    partNumber: partNumber,
                    name: name,
                    dimensions: dimensions,
                    bearingTypeId: type.Id,
                    brandId: brand.Id,
                    performance: performance,
                    weight: weight);
            }
        }
    }
}
