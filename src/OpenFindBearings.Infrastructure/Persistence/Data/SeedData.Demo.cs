using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    /// <summary>
    /// 开发环境演示数据：示例用户、示例轴承与替代关系。
    /// 改动说明：演示数据只在 Development 且相关表为空时写入，供开源仓与闭源仓本机联调形成完整闭环；
    ///   生产与自部署环境不受影响，也不需要任何开关。
    /// </summary>
    public static partial class SeedData
    {
        /// <summary>演示用户使用的固定外部认证 Id，便于重复启动时判重。</summary>
        private static readonly string[] DemoAuthUserIds =
        [
            "auth-merchant-001",
            "auth-merchant-002",
            "auth-customer-001",
            "auth-customer-002",
        ];

        /// <summary>
        /// 幂等写入演示数据：用户与轴承各自判重，已存在则跳过。
        /// </summary>
        private static async Task EnsureDemoDataAsync(ApplicationDbContext context, ILogger logger)
        {
            await EnsureDemoUsersAsync(context, logger);
            await EnsureDemoBearingsAsync(context, logger);
        }

        /// <summary>
        /// 演示用户：两个商户经营者与两个普通用户，统一挂 Individual 身份角色（商户身份由成员表承载）。
        /// </summary>
        private static async Task EnsureDemoUsersAsync(ApplicationDbContext context, ILogger logger)
        {
            var existingAuthIds = await context.Users
                .Where(u => DemoAuthUserIds.Contains(u.AuthUserId))
                .Select(u => u.AuthUserId)
                .ToListAsync();

            if (existingAuthIds.Count == DemoAuthUserIds.Length)
            {
                return;
            }

            var individualRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Individual");
            if (individualRole is null)
            {
                logger.LogWarning("演示用户跳过：缺少 Individual 角色");
                return;
            }

            var nicknames = new Dictionary<string, string>
            {
                ["auth-merchant-001"] = "张经理",
                ["auth-merchant-002"] = "李经理",
                ["auth-customer-001"] = "王先生",
                ["auth-customer-002"] = "赵女士",
            };

            var added = 0;
            foreach (var authUserId in DemoAuthUserIds)
            {
                if (existingAuthIds.Contains(authUserId))
                {
                    continue;
                }

                var user = new User(authUserId, RegistrationSource.Mobile, null, nicknames[authUserId]);
                context.Users.Add(user);
                context.UserRoles.Add(new UserRole(user.Id, individualRole.Id));
                added++;
            }

            if (added > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("演示用户已写入 {Count} 个", added);
            }
        }

        /// <summary>
        /// 补齐演示轴承所需的六个常见品牌。这些品牌通常来自爬取数据源，空库不存在；
        /// 仅开发环境创建，来源标记为种子数据，同步任务不会覆盖。
        /// </summary>
        private static async Task EnsureDemoBrandsAsync(ApplicationDbContext context)
        {
            var required = new (string Code, string Name, BrandLevel Level)[]
            {
                ("SKF", "SKF", BrandLevel.InternationalPremium),
                ("FAG", "FAG", BrandLevel.InternationalPremium),
                ("NSK", "NSK", BrandLevel.InternationalPremium),
                ("HRB", "HRB", BrandLevel.DomesticPremium),
                ("ZWZ", "ZWZ", BrandLevel.DomesticPremium),
                ("LYC", "LYC", BrandLevel.DomesticPremium),
            };

            var codes = required.Select(r => r.Code).ToArray();
            var existing = await context.Brands
                .Where(b => codes.Contains(b.Code))
                .Select(b => b.Code)
                .ToListAsync();

            var added = false;
            foreach (var (code, name, level) in required)
            {
                if (existing.Contains(code))
                {
                    continue;
                }

                var brand = new Brand(code, name, level);
                brand.SetDataSource(DataSource.FromSeedData());
                context.Brands.Add(brand);
                added = true;
            }

            if (added)
            {
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 演示轴承与替代关系：依赖主数据里的品牌与类型，只在轴承表为空时写入，避免重复启动翻倍。
        /// </summary>
        private static async Task EnsureDemoBearingsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Bearings.AnyAsync())
            {
                return;
            }

            // 演示轴承依赖这六个常见品牌；它们来自爬取源、空库并不存在，这里按需补建（仅开发环境）
            await EnsureDemoBrandsAsync(context);

            var brands = await context.Brands
                .Where(b => new[] { "SKF", "FAG", "NSK", "HRB", "ZWZ", "LYC" }.Contains(b.Code))
                .ToListAsync();
            var types = await context.BearingTypes
                .Where(t => new[] { "DGBB", "ACBB" }.Contains(t.Code))
                .ToListAsync();

            var skf = brands.FirstOrDefault(b => b.Code == "SKF");
            var fag = brands.FirstOrDefault(b => b.Code == "FAG");
            var nsk = brands.FirstOrDefault(b => b.Code == "NSK");
            var hrb = brands.FirstOrDefault(b => b.Code == "HRB");
            var zwz = brands.FirstOrDefault(b => b.Code == "ZWZ");
            var lyc = brands.FirstOrDefault(b => b.Code == "LYC");
            var dgbb = types.FirstOrDefault(t => t.Code == "DGBB");
            var acbb = types.FirstOrDefault(t => t.Code == "ACBB");

            if (skf is null || fag is null || nsk is null || hrb is null || zwz is null || lyc is null
                || dgbb is null || acbb is null)
            {
                logger.LogWarning("演示轴承跳过：主数据品牌或类型不完整");
                return;
            }

            var bearings = new List<Bearing>
            {
                Bearing.CreateBearing("6205", dgbb.Id, dgbb.Name, 25, 52, 15, skf.Id, 0.12m),
                Bearing.CreateBearing("6206", dgbb.Id, dgbb.Name, 30, 62, 16, skf.Id, 0.15m),
                Bearing.CreateBearing("6305", dgbb.Id, dgbb.Name, 25, 62, 17, skf.Id, 0.17m),
                Bearing.CreateBearing("6310", dgbb.Id, dgbb.Name, 50, 110, 27, skf.Id, 0.85m),

                Bearing.CreateBearing("6205", dgbb.Id, dgbb.Name, 25, 52, 15, fag.Id, 0.12m),
                Bearing.CreateBearing("6305", dgbb.Id, dgbb.Name, 25, 62, 17, fag.Id, 0.17m),
                Bearing.CreateBearing("7205-B", acbb.Id, acbb.Name, 25, 52, 15, fag.Id, 0.13m),

                Bearing.CreateBearing("6205", dgbb.Id, dgbb.Name, 25, 52, 15, nsk.Id, 0.12m),
                Bearing.CreateBearing("6205DU", dgbb.Id, dgbb.Name, 25, 52, 15, nsk.Id, 0.12m),
                Bearing.CreateBearing("7205", acbb.Id, acbb.Name, 25, 52, 15, nsk.Id, 0.13m),

                Bearing.CreateBearing("6205-2RS", dgbb.Id, dgbb.Name, 25, 52, 15, hrb.Id, 0.12m),
                Bearing.CreateBearing("6205-Z", dgbb.Id, dgbb.Name, 25, 52, 15, hrb.Id, 0.12m),
                Bearing.CreateBearing("6305", dgbb.Id, dgbb.Name, 25, 62, 17, hrb.Id, 0.17m),

                Bearing.CreateBearing("6205", dgbb.Id, dgbb.Name, 25, 52, 15, zwz.Id, 0.12m),
                Bearing.CreateBearing("6206", dgbb.Id, dgbb.Name, 30, 62, 16, zwz.Id, 0.15m),

                Bearing.CreateBearing("6205", dgbb.Id, dgbb.Name, 25, 52, 15, lyc.Id, 0.12m),
                Bearing.CreateBearing("6310", dgbb.Id, dgbb.Name, 50, 110, 27, lyc.Id, 0.85m),
            };

            // 技术参数与产地按品牌分组统一设置，顺序与上方创建顺序严格对应
            var specs = new (string Precision, string Material, string Seal, string Cage)[]
            {
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "ZZ", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "2RS", "钢保持架"),
                ("P0", "GCr15", "Z", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
                ("P0", "GCr15", "Open", "钢保持架"),
            };

            var origins = new (string Origin, BearingCategory Category)[]
            {
                ("瑞典", BearingCategory.Imported),
                ("瑞典", BearingCategory.Imported),
                ("瑞典", BearingCategory.Imported),
                ("瑞典", BearingCategory.Imported),
                ("德国", BearingCategory.Imported),
                ("德国", BearingCategory.Imported),
                ("德国", BearingCategory.Imported),
                ("日本", BearingCategory.Imported),
                ("日本", BearingCategory.Imported),
                ("日本", BearingCategory.Imported),
                ("中国", BearingCategory.Domestic),
                ("中国", BearingCategory.Domestic),
                ("中国", BearingCategory.Domestic),
                ("中国", BearingCategory.Domestic),
                ("中国", BearingCategory.Domestic),
                ("中国", BearingCategory.Domestic),
                ("中国", BearingCategory.Domestic),
            };

            for (var i = 0; i < bearings.Count; i++)
            {
                var spec = specs[i];
                bearings[i].UpdateTechnicalSpecs(spec.Precision, spec.Material, spec.Seal, spec.Cage);
                var origin = origins[i];
                bearings[i].SetOrigin(origin.Origin, origin.Category);
            }

            await context.Bearings.AddRangeAsync(bearings);
            await context.SaveChangesAsync();

            logger.LogInformation("演示轴承已写入 {Count} 条", bearings.Count);

            var skf6205 = bearings[0];
            var fag6205 = bearings[4];
            var nsk6205 = bearings[7];
            var hrb6205Rs = bearings[10];
            var zwz6205 = bearings[13];
            var lyc6205 = bearings[15];
            var skf6305 = bearings[2];

            var interchanges = new List<BearingInterchange>
            {
                new(skf6205.Id, fag6205.Id, "exact", 100, "SKF官方互换表", "完全替代", true),
                new(skf6205.Id, nsk6205.Id, "exact", 100, "SKF官方互换表", "完全替代", true),
                new(skf6205.Id, hrb6205Rs.Id, "exact", 95, "国标互换手册", "带密封圈", true),
                new(skf6205.Id, zwz6205.Id, "exact", 90, "国标互换手册", "完全替代", true),
                new(skf6205.Id, lyc6205.Id, "exact", 90, "国标互换手册", "完全替代", true),
                new(skf6205.Id, skf6305.Id, "conditional", 60, "尺寸相近", "内径相同，外径和宽度更大", false),
            };

            await context.BearingInterchanges.AddRangeAsync(interchanges);
            await context.SaveChangesAsync();

            logger.LogInformation("演示替代关系已写入 {Count} 条", interchanges.Count);
        }
    }
}