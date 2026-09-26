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

                // 改动说明：迁移已上移到 Program.cs 的启动块统一执行（失败即抛出、快速暴露），
                //   此处不再重复 MigrateAsync——原先它被外层 try/catch 吞异常，正是生产库曾停在
                //   InitialCreate、缺 MerchantMembers 表导致相关接口 500 的根因。SeedData 只负责种子。

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
            // 改动说明（v1.39.0 权限目录重排）：三段式"资源.动作"目录，与存量库迁移
            // RebalancePermissionCatalog 双轨一致。拆分：品牌/类型/映射/任务/积分独立键；
            // sync.review 更名 review.sync；认证管理拆 view/ban/manage/assign 四级（封禁可下放
            // 操作员、角色分配 Admin 专属防提权）；删除 app 侧僵尸键 correction.submit/favorite.*
            // （app 权限模型=登录态+业务资格，不走 RBAC）
            var permissions = new List<Permission>
            {
                new("dashboard.view", "查看仪表盘"),
                new("bearing.view", "查看轴承"),
                new("bearing.create", "创建轴承"),
                new("bearing.edit", "编辑轴承"),
                new("bearing.delete", "删除轴承"),
                new("brand.view", "查看品牌"),
                new("brand.create", "创建品牌"),
                new("brand.edit", "编辑品牌"),
                new("brand.delete", "删除品牌"),
                new("type.view", "查看型号"),
                new("type.create", "创建型号"),
                new("type.edit", "编辑型号"),
                new("type.delete", "删除型号"),
                new("merchant.view", "查看商家"),
                new("merchant.manage", "编辑商家"),
                new("merchant.verify", "认证审核商家"),
                new("merchant.detach", "解除商家归属"),
                new("merchant.import", "商家库存导入"),
                new("data.restore", "恢复已删数据"),
                new("data.harddelete", "彻底删除数据"),
                new("mapping.view", "查看映射维护"),
                new("mapping.manage", "管理映射关系"),
                new("review.sync", "同步数据审核"),
                new("correction.review", "审核纠错"),
                new("sourcing.view", "查看寻货"),
                new("sourcing.manage", "治理寻货"),
                new("sync.run", "触发爬虫任务"),
                new("user.view", "查看用户"),
                new("user.ban", "封禁与解禁用户"),
                new("user.manage", "管理用户账号"),
                new("user.assign", "分配平台角色"),
                new("role.manage", "管理角色"),
                new("permission.view", "查看权限清单"),
                new("system.view", "查看系统配置"),
                new("system.manage", "修改系统配置"),
                new("points.manage", "配置积分任务"),
                new("audit.view", "查看审计日志"),
            };
            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            // 创建角色
            // 改动说明（v1.31.0）：删除 MerchantAdmin/MerchantStaff 僵尸种子——商户鉴权只信
            //   MerchantMembers 成员表（PermissionService.IsMerchantAdmin），Roles 表同名角色
            //   永无赋值与消费方；存量库由迁移清理。新增 Operator/Auditor 落地轻量三员分立：
            //   管理员=全权、操作员=日常 CRUD+审核、审计员=只读+审计日志
            // 改动说明（v1.38.0）：第 4 参为中文显示名（Name 保持英文机器标识），与存量库迁移
            // AddRoleDisplayName 的 UPDATE 保持一致，新装/存量两轨同名
            var roles = new List<Role>
            {
                new("Admin", "平台管理员", true, "管理员"),
                new("Operator", "操作员", true, "操作员"),
                new("Auditor", "审计员", true, "审计员"),
                new("Individual", "个人用户", true, "普通用户")
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            // 分配权限给角色
            var adminRole = roles.First(r => r.Name == "Admin");
            var operatorRole = roles.First(r => r.Name == "Operator");
            var auditorRole = roles.First(r => r.Name == "Auditor");
            // 开发种子用户挂 Individual 身份角色（v1.39.0：Individual 零权限点，仅作前端用户身份标记）
            var individualRole = roles.First(r => r.Name == "Individual");

            var rolePermissions = new List<RolePermission>();

            // Admin 拥有所有权限
            foreach (var permission in permissions)
            {
                rolePermissions.Add(new RolePermission(adminRole.Id, permission.Id));
            }

            // Operator（v1.39.0 重定义）= 仪表盘 + 数据管理 view/create/edit（无删除/恢复/解除归属）
            // + 审核组全量 + 映射查看 + 用户封禁（客服常规处置）；
            // 刻意不含：delete/restore/harddelete/detach/mapping.manage/sync.run/user.assign/role.manage
            string[] operatorPerms = [
                "dashboard.view",
                "bearing.view", "bearing.create", "bearing.edit",
                "brand.view", "brand.create", "brand.edit",
                "type.view", "type.create", "type.edit",
                "merchant.view", "merchant.manage", "merchant.verify", "merchant.import",
                "mapping.view",
                "review.sync", "correction.review", "sourcing.view", "sourcing.manage",
                "user.view", "user.ban",
            ];
            foreach (var name in operatorPerms)
            {
                rolePermissions.Add(new RolePermission(operatorRole.Id, permissions.First(p => p.Name == name).Id));
            }

            // Auditor = 只读监察：全部 view 键 + 审计日志（无任何写权限点）
            string[] auditorPerms = [
                "dashboard.view", "bearing.view", "brand.view", "type.view", "merchant.view",
                "mapping.view", "review.sync", "correction.review", "sourcing.view",
                "user.view", "permission.view", "system.view", "audit.view",
            ];
            foreach (var name in auditorPerms)
            {
                rolePermissions.Add(new RolePermission(auditorRole.Id, permissions.First(p => p.Name == name).Id));
            }

            // Individual 零权限点（v1.39.0）：app 能力=登录态+业务资格，不走 RBAC；
            // 角色仅作"前端用户"身份标记（Admin 用户页签归类、后台登录 gate 拒绝依据）
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
                    // 改动说明（v1.31.0）：MerchantAdmin 平台角色已删（商户鉴权走成员表），
                    // 开发种子商户用户统一落 Individual，商户身份由 MerchantMembers 承载
                    new UserRole(merchant1.Id, individualRole.Id),
                    new UserRole(merchant2.Id, individualRole.Id),
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
            ("Site.BeiAn", "", "Site", "网站备案号（Admin 页脚与 H5 端关于页）", "string", true),
            ("Site.CustomerService", "", "Site", "客服电话（全端共用：app 设置页拨打与法务文本）", "string", true),

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

            // 移动端版本更新（供 /api/mobile/version/check 消费；由 Taro 集群侧 CronJob apk-sync 拉包齐备后自动改写，Admin 页手动兜底）
            ("Mobile.AppVersion", "v1.0.0-rc.1", "Mobile", "移动端最新版本号（SemVer 带点 prerelease，如 v1.0.0-rc.2）", "string", true),
            ("Mobile.MinVersion", "v1.0.0-rc.1", "Mobile", "最低支持版本（低于此值且开启强更时强制更新）", "string", true),
            ("Mobile.ForceUpdate", "false", "Mobile", "强制更新开关", "bool", true),
            ("Mobile.DownloadUrl", "https://bff.515813.xyz/dl/", "Mobile", "APK 下载目录（以/结尾，客户端按 app-v版本-ABI.apk 拼文件名；H5/小程序无需填写）", "string", true),
            ("Mobile.UpdateMessage", "发现新版本，建议更新", "Mobile", "更新弹窗说明文案", "string", true),
            // 媒体资源公网 base：图片经独立 nginx 媒体服务在 /media 直出（应用不再逐字节代理）；
            // 库内只存相对键（/images、/uploads、/avatars），客户端拼此 base。将来切对象存储只改此值
            ("Mobile.MediaBaseUrl", "https://bff.515813.xyz/media", "Mobile", "媒体资源公网 base（末尾无斜杠）", "string", true),
            // 改动说明（v1.37.0 备案拆分）：工信部 App/小程序/网站独立备案各出号，App 与小程序关于页
            // 分别消费下列两键（空=该行隐藏）；网站备案沿用 Site.BeiAn
            ("Mobile.BeiAnApp", "", "Mobile", "App 备案号（RN 端关于页展示）", "string", true),
            ("Mobile.BeiAnMini", "", "Mobile", "小程序备案号（微信端关于页展示）", "string", true),
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
