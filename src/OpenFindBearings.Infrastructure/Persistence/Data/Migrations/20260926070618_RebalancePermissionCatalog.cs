using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebalancePermissionCatalog : Migration
    {
        /// <summary>
        /// 权限目录重排（v1.39.0），与 SeedData 新装库目录双轨一致：
        /// 1. sync.review 更名 review.sync（授权按 Id 关联，改名不断链）
        /// 2. 新增 17 键：品牌/类型/映射/任务/积分从借用键拆出独立控制；
        ///    认证管理拆 user.view/user.ban/user.assign（封禁可下放操作员、
        ///    角色分配 Admin 专属防提权）；merchant.import/permission.view 新启用
        /// 3. Admin 补齐全部新键授权（Admin=全量语义不变）
        /// 4. Operator/Auditor 先清后按新语义重挂（Operator 去 delete/restore/detach/sync.run，
        ///    补 brand/type/mapping view+create+edit、user.view/user.ban；Auditor 扩为全 view+audit）
        /// 5. 删除 app 侧僵尸键 correction.submit/favorite.*（app 权限模型=登录态+业务资格）
        /// 全程幂等（NOT EXISTS 防重），列清单含 BaseEntity 全部 NOT NULL 列
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) 更名：同步数据审核键
            migrationBuilder.Sql("""
                UPDATE "Permissions" SET "Name" = 'review.sync', "Description" = '同步数据审核'
                WHERE "Name" = 'sync.review';
                """);

            // 2) 新增键（幂等补插）
            migrationBuilder.Sql("""
                INSERT INTO "Permissions" ("Id", "Name", "Description", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), v."Name", v."Description", timezone('utc', now()), true
                FROM (VALUES
                    ('brand.view', '查看品牌'),
                    ('brand.create', '创建品牌'),
                    ('brand.edit', '编辑品牌'),
                    ('brand.delete', '删除品牌'),
                    ('type.view', '查看型号'),
                    ('type.create', '创建型号'),
                    ('type.edit', '编辑型号'),
                    ('type.delete', '删除型号'),
                    ('merchant.import', '商家库存导入'),
                    ('mapping.view', '查看映射维护'),
                    ('mapping.manage', '管理映射关系'),
                    ('sync.run', '触发爬虫任务'),
                    ('user.view', '查看用户'),
                    ('user.ban', '封禁与解禁用户'),
                    ('user.assign', '分配平台角色'),
                    ('permission.view', '查看权限清单'),
                    ('points.manage', '配置积分任务')
                ) AS v("Name", "Description")
                WHERE NOT EXISTS (SELECT 1 FROM "Permissions" p WHERE p."Name" = v."Name");
                """);

            // 3) Admin 补齐所有未授权限（含全部新键，维持 Admin=全量）
            migrationBuilder.Sql("""
                INSERT INTO "RolePermissions" ("Id", "RoleId", "PermissionId", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), r."Id", p."Id", timezone('utc', now()), true
                FROM "Roles" r, "Permissions" p
                WHERE r."Name" = 'Admin'
                  AND NOT EXISTS (
                      SELECT 1 FROM "RolePermissions" rp
                      WHERE rp."RoleId" = r."Id" AND rp."PermissionId" = p."Id");
                """);

            // 4) Operator/Auditor 先清后重挂（角色不存在时 DELETE/INSERT 均空转，幂等安全）
            migrationBuilder.Sql("""
                DELETE FROM "RolePermissions" rp
                USING "Roles" r
                WHERE rp."RoleId" = r."Id" AND r."Name" IN ('Operator', 'Auditor');
                """);
            migrationBuilder.Sql("""
                INSERT INTO "RolePermissions" ("Id", "RoleId", "PermissionId", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), r."Id", p."Id", timezone('utc', now()), true
                FROM "Roles" r
                JOIN "Permissions" p ON p."Name" IN (
                    'dashboard.view',
                    'bearing.view', 'bearing.create', 'bearing.edit',
                    'brand.view', 'brand.create', 'brand.edit',
                    'type.view', 'type.create', 'type.edit',
                    'merchant.view', 'merchant.manage', 'merchant.verify', 'merchant.import',
                    'mapping.view',
                    'review.sync', 'correction.review', 'sourcing.view', 'sourcing.manage',
                    'user.view', 'user.ban'
                )
                WHERE r."Name" = 'Operator';
                """);
            migrationBuilder.Sql("""
                INSERT INTO "RolePermissions" ("Id", "RoleId", "PermissionId", "CreatedAt", "IsActive")
                SELECT gen_random_uuid(), r."Id", p."Id", timezone('utc', now()), true
                FROM "Roles" r
                JOIN "Permissions" p ON p."Name" IN (
                    'dashboard.view', 'bearing.view', 'brand.view', 'type.view', 'merchant.view',
                    'mapping.view', 'review.sync', 'correction.review', 'sourcing.view',
                    'user.view', 'permission.view', 'system.view', 'audit.view'
                )
                WHERE r."Name" = 'Auditor';
                """);

            // 5) 删除僵尸键及其授权（app 端点不走 RBAC，此三键从无守卫消费方）
            migrationBuilder.Sql("""
                DELETE FROM "RolePermissions"
                WHERE "PermissionId" IN (
                    SELECT "Id" FROM "Permissions"
                    WHERE "Name" IN ('correction.submit', 'favorite.bearing', 'favorite.merchant'));
                """);
            migrationBuilder.Sql("""
                DELETE FROM "Permissions"
                WHERE "Name" IN ('correction.submit', 'favorite.bearing', 'favorite.merchant');
                """);

            // Individual 授权清空（v1.39.0 起零权限点，仅作前端用户身份标记）
            migrationBuilder.Sql("""
                DELETE FROM "RolePermissions" rp
                USING "Roles" r
                WHERE rp."RoleId" = r."Id" AND r."Name" = 'Individual';
                """);
        }

        /// <summary>
        /// 逆操作仅恢复键名与目录骨架；Operator/Auditor 旧授权集不可精确还原
        /// （语义变更属单向演进），回滚后需管理员在角色页重新手工配置
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Permissions" SET "Name" = 'sync.review' WHERE "Name" = 'review.sync';
                """);
            migrationBuilder.Sql("""
                DELETE FROM "RolePermissions"
                WHERE "PermissionId" IN (
                    SELECT "Id" FROM "Permissions"
                    WHERE "Name" IN ('brand.view','brand.create','brand.edit','brand.delete',
                        'type.view','type.create','type.edit','type.delete',
                        'merchant.import','mapping.view','mapping.manage','sync.run',
                        'user.view','user.ban','user.assign','permission.view','points.manage'));
                """);
            migrationBuilder.Sql("""
                DELETE FROM "Permissions"
                WHERE "Name" IN ('brand.view','brand.create','brand.edit','brand.delete',
                    'type.view','type.create','type.edit','type.delete',
                    'merchant.import','mapping.view','mapping.manage','sync.run',
                    'user.view','user.ban','user.assign','permission.view','points.manage');
                """);
        }
    }
}
