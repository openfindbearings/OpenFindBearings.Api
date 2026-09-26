using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 改动说明：xmin 为 PostgreSQL 系统列（PointAccounts 并发令牌），本就存在，
            // 工具误生成 AddColumn 会失败，故移除；并发令牌为纯模型映射无需 DDL

            migrationBuilder.CreateTable(
                name: "AchievementDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProgressTarget = table.Column<int>(type: "integer", nullable: false),
                    MetaPoints = table.Column<int>(type: "integer", nullable: false),
                    RewardPoints = table.Column<int>(type: "integer", nullable: false),
                    TitleReward = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Rare = table.Column<bool>(type: "boolean", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchievementUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementUnlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AchievementDefinitions_Key",
                table: "AchievementDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AchievementUnlocks_Owner_Key",
                table: "AchievementUnlocks",
                columns: new[] { "Scope", "OwnerId", "AchievementKey" },
                unique: true);

            // 改动说明（v2.1.0 成就子系统）：初始成就目录 + 成就解锁发分规则种子走迁移而非
            // SeedData——存量库 SeedData 按 Users 非空整体短路永不执行，迁移是唯一可靠落点；
            // 幂等 NOT EXISTS。Scope: 1=Personal 2=Merchant
            migrationBuilder.Sql(@"
INSERT INTO ""AchievementDefinitions"" (""Id"",""Key"",""Name"",""Description"",""Icon"",""Scope"",""Category"",""MetricKey"",""ProgressTarget"",""MetaPoints"",""RewardPoints"",""TitleReward"",""Rare"",""Hidden"",""Enabled"",""CreatedAt"",""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Name"", v.""Description"", v.""Icon"", v.""Scope"", v.""Category"", v.""MetricKey"", v.""Target"", v.""Meta"", v.""Reward"", v.""Title"", v.""Rare"", v.""Hidden"", true, now(), true
FROM (VALUES
 ('correction_first','初次纠错','首条纠错被平台采纳','edit',1,'数据共创','correction_adopted',1,10,5,NULL,false,false),
 ('correction_10','火眼金睛','累计 10 条纠错被采纳','search',1,'数据共创','correction_adopted',10,30,10,NULL,false,false),
 ('correction_50','数据纠错师','累计 50 条纠错被采纳','award',1,'数据共创','correction_adopted',50,80,20,'数据纠错师',false,false),
 ('correction_100','数据守护神','累计 100 条纠错被采纳','shield',1,'数据共创','correction_adopted',100,150,30,'数据守护神',true,false),
 ('checkin_7','七日之约','连续签到 7 天','calendar',1,'忠诚','checkin_streak',7,15,5,NULL,false,false),
 ('checkin_30','月度坚守','连续签到 30 天','calendar-check',1,'忠诚','checkin_streak',30,40,10,'坚守者',false,false),
 ('checkin_100','百日之约','连续签到 100 天','star',1,'忠诚','checkin_streak',100,100,20,NULL,true,false),
 ('checkin_total_30','签到达人','累计签到 30 次','check',1,'忠诚','checkin_total',30,20,5,NULL,false,false),
 ('sourcing_selected_first','伯乐','应答首次被发布人选定','handshake',1,'寻货','sourcing_selected',1,20,10,NULL,false,false),
 ('sourcing_selected_10','金牌红娘','累计 10 次应答被选定','medal',1,'寻货','sourcing_selected',10,60,20,'金牌红娘',false,false),
 ('merchant_first_listing','开张大吉','首件商品上架在售','store',2,'商户','listing_count',1,20,0,NULL,false,false),
 ('merchant_listing_20','货架满满','在售商品达 20 件','boxes',2,'商户','listing_count',20,50,0,NULL,false,false),
 ('merchant_verified','认证商家','通过平台资质认证','badge-check',2,'商户','merchant_verified',1,40,0,'认证商家',false,false),
 ('merchant_selected_first','首单中选','应答首次被发布人选定','handshake',2,'商户','merchant_selected',1,20,0,NULL,false,false),
 ('merchant_selected_10','金牌供应商','累计 10 次应答被选定','trophy',2,'商户','merchant_selected',10,60,0,NULL,true,false)
) AS v(""Key"",""Name"",""Description"",""Icon"",""Scope"",""Category"",""MetricKey"",""Target"",""Meta"",""Reward"",""Title"",""Rare"",""Hidden"")
WHERE NOT EXISTS (SELECT 1 FROM ""AchievementDefinitions"" a WHERE a.""Key"" = v.""Key"");

INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'achievement_unlock', '成就解锁奖励', 5, 0, NULL, true, '成就点亮一次性可花积分甜头（实际分值按成就定义覆盖）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'achievement_unlock');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementDefinitions");

            migrationBuilder.DropTable(
                name: "AchievementUnlocks");
        }
    }
}
