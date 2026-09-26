using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    // 改动说明：手写迁移必须带 [Migration] 特性——EF 靠该特性（而非文件名）发现迁移类，
    // 缺它会被 MigrateAsync 静默跳过（rc.30 迁移未执行的根因；纯 SQL 迁移无需 Designer/快照）
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260926090000_AddBusinessTimeZoneAndCorrectionScore")]
    public partial class AddBusinessTimeZoneAndCorrectionScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v1.36.1 日界修复配套：
            // ① 业务时区偏移配置种子（默认 8=北京时间）——BusinessClock 启动时读取，
            //    Admin 系统配置页可改（WordPress options 同款模式）；幂等 NOT EXISTS，
            //    列集合含 BaseEntity 全部 NOT NULL 列（Id/CreatedAt/IsActive，缺列即启动迁移崩溃的教训）
            // ② 纠错被采纳分值 10→20 存量对齐——AddPointSystem 种子改默认值只影响新装库，
            //    存量库必须 UPDATE（仅当仍为旧默认 10 时提升，管理员自定义过的值不动）
            migrationBuilder.Sql(@"
INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(),
       'Business.TimeZoneOffsetHours',
       '8',
       '业务时区偏移小时数：日任务/签到/额度的切日边界（默认 8=北京时间）。修改后需重启 API 生效（避免滚动期各副本日界不一致导致幂等键漂移）',
       'business',
       'int',
       false,
       now(),
       true
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" WHERE ""Key"" = 'Business.TimeZoneOffsetHours');

UPDATE ""PointGrantRules"" SET ""Amount"" = 20, ""UpdatedAt"" = now()
WHERE ""GrantType"" = 'correction_adopted' AND ""Amount"" = 10;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""SystemConfigs"" WHERE ""Key"" = 'Business.TimeZoneOffsetHours';

UPDATE ""PointGrantRules"" SET ""Amount"" = 10, ""UpdatedAt"" = now()
WHERE ""GrantType"" = 'correction_adopted' AND ""Amount"" = 20;");
        }
    }
}
