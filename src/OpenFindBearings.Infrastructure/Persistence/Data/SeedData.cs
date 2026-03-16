using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 检查是否已有数据
            if (await context.Brands.AnyAsync())
                return;

            // ============ 1. 基础字典数据 ============

            // 添加品牌
            var brands = new List<Brand>
            {
                new("SKF", "SKF", BrandLevel.InternationalPremium),
                new("FAG", "FAG", BrandLevel.InternationalPremium),
                new("NSK", "NSK", BrandLevel.InternationalPremium),
                new("HRB", "HRB", BrandLevel.DomesticPremium),
                new("ZWZ", "ZWZ", BrandLevel.DomesticPremium),
                new("LYC", "LYC", BrandLevel.DomesticPremium)
            };

            await context.Brands.AddRangeAsync(brands);
            await context.SaveChangesAsync();

            // 添加轴承类型
            var bearingTypes = new List<BearingType>
            {
                new("DGBB", "深沟球轴承", "最常用的滚动轴承，主要承受径向载荷"),
                new("ACBB", "角接触球轴承", "可同时承受径向和轴向载荷"),
                new("SRB", "调心滚子轴承", "具有调心功能，适用于重载"),
                new("TRB", "圆锥滚子轴承", "可承受径向和轴向联合载荷")
            };

            await context.BearingTypes.AddRangeAsync(bearingTypes);
            await context.SaveChangesAsync();

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
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "product.view").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "product.create").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "product.edit").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "product.delete").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "merchant.view").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "merchant.verify").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "merchant.manage").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "correction.review").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "user.manage").Id),
                new RolePermission(globalAdmin.Id, permissions.First(p => p.Name == "role.manage").Id),

                // MerchantAdmin 拥有商家管理权限
                new RolePermission(merchantAdmin.Id, permissions.First(p => p.Name == "product.view").Id),
                new RolePermission(merchantAdmin.Id, permissions.First(p => p.Name == "product.create").Id),
                new RolePermission(merchantAdmin.Id, permissions.First(p => p.Name == "product.edit").Id),
                new RolePermission(merchantAdmin.Id, permissions.First(p => p.Name == "merchant.view").Id),

                // MerchantStaff 拥有查看权限
                new RolePermission(merchantStaff.Id, permissions.First(p => p.Name == "product.view").Id),
                new RolePermission(merchantStaff.Id, permissions.First(p => p.Name == "merchant.view").Id),

                // Customer 只有基本权限
                new RolePermission(customer.Id, permissions.First(p => p.Name == "product.view").Id),
                new RolePermission(customer.Id, permissions.First(p => p.Name == "correction.submit").Id),
            };

            await context.RolePermissions.AddRangeAsync(rolePermissions);
            await context.SaveChangesAsync();

            // ============ 3. 轴承产品数据（使用Dimensions和PerformanceParams） ============

            var bearings = new List<Bearing>();

            // 辅助方法：创建轴承
            Bearing CreateBearing(
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

            // SKF 品牌的产品
            bearings.Add(CreateBearing("6205", "SKF 深沟球轴承 6205", 25, 52, 15,
                bearingTypes[0], brands[0], weight: 0.12m));
            bearings.Add(CreateBearing("6206", "SKF 深沟球轴承 6206", 30, 62, 16,
                bearingTypes[0], brands[0], weight: 0.15m));
            bearings.Add(CreateBearing("6305", "SKF 深沟球轴承 6305", 25, 62, 17,
                bearingTypes[0], brands[0], weight: 0.17m));
            bearings.Add(CreateBearing("6310", "SKF 深沟球轴承 6310", 50, 110, 27,
                bearingTypes[0], brands[0], weight: 0.85m));

            // FAG 品牌的产品
            bearings.Add(CreateBearing("6205", "FAG 深沟球轴承 6205", 25, 52, 15,
                bearingTypes[0], brands[1], weight: 0.12m));
            bearings.Add(CreateBearing("6305", "FAG 深沟球轴承 6305", 25, 62, 17,
                bearingTypes[0], brands[1], weight: 0.17m));
            bearings.Add(CreateBearing("7205-B", "FAG 角接触球轴承 7205-B", 25, 52, 15,
                bearingTypes[1], brands[1], weight: 0.13m));

            // NSK 品牌的产品
            bearings.Add(CreateBearing("6205", "NSK 深沟球轴承 6205", 25, 52, 15,
                bearingTypes[0], brands[2], weight: 0.12m));
            bearings.Add(CreateBearing("6205DU", "NSK 深沟球轴承 6205DU", 25, 52, 15,
                bearingTypes[0], brands[2], weight: 0.12m));
            bearings.Add(CreateBearing("7205", "NSK 角接触球轴承 7205", 25, 52, 15,
                bearingTypes[1], brands[2], weight: 0.13m));

            // HRB 品牌的产品
            bearings.Add(CreateBearing("6205-2RS", "HRB 深沟球轴承 6205-2RS", 25, 52, 15,
                bearingTypes[0], brands[3], weight: 0.12m));
            bearings.Add(CreateBearing("6205-Z", "HRB 深沟球轴承 6205-Z", 25, 52, 15,
                bearingTypes[0], brands[3], weight: 0.12m));
            bearings.Add(CreateBearing("6305", "HRB 深沟球轴承 6305", 25, 62, 17,
                bearingTypes[0], brands[3], weight: 0.17m));

            // ZWZ 品牌的产品
            bearings.Add(CreateBearing("6205", "ZWZ 深沟球轴承 6205", 25, 52, 15,
                bearingTypes[0], brands[4], weight: 0.12m));
            bearings.Add(CreateBearing("6206", "ZWZ 深沟球轴承 6206", 30, 62, 16,
                bearingTypes[0], brands[4], weight: 0.15m));

            // LYC 品牌的产品
            bearings.Add(CreateBearing("6205", "LYC 深沟球轴承 6205", 25, 52, 15,
                bearingTypes[0], brands[5], weight: 0.12m));
            bearings.Add(CreateBearing("6310", "LYC 深沟球轴承 6310", 50, 110, 27,
                bearingTypes[0], brands[5], weight: 0.85m));

            await context.Bearings.AddRangeAsync(bearings);
            await context.SaveChangesAsync();

            // 补充技术参数（独立字段）
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

            await context.SaveChangesAsync();

            // ============ 4. 替代品关系 ============

            // 查找各品牌的6205
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

            // ============ 5. 商家数据 ============

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

            // ============ 6. 商家-产品关联 ============

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
                new(merchants[4].Id, fag6205.Id, "¥60-68", "期货"),
                new(merchants[4].Id, nsk6205.Id, "¥58-65", "期货"),
                new(merchants[4].Id, bearings[6].Id, "¥75-85", "现货")
            };

            // 设置一些推荐产品
            merchantBearings[0].SetFeatured(true);
            merchantBearings[3].SetFeatured(true);
            merchantBearings[4].SetFeatured(true);

            await context.MerchantBearings.AddRangeAsync(merchantBearings);
            await context.SaveChangesAsync();
        }
    }
}
