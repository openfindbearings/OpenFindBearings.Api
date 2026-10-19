using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 寻货曝光范围迁移（v1.8.0 公开招标/邀标）：TargetMerchantId 单列升级为
    /// Visibility（0=公开招标 / 1=邀标）+ InvitedMerchantIds（邀标收件人快照 uuid[]）。
    /// 采用"先加新列 → 回填存量 → 再删旧列"的幂等 SQL 手动编排（EF 自动生成的顺序会先删列，
    /// 会把存量定向单信息删丢）；Down 反向回填单收件人。
    /// </summary>
    public partial class AddSourcingVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 改动说明（v1.8.0 公开招标/邀标）：全部幂等——列默认值让存量行先落在公开侧，
            //   再把原定向单（TargetMerchantId 非空）回填为邀标 + 收件人=[该商户]，最后才删旧列
            migrationBuilder.Sql(@"
                ALTER TABLE ""SourcingDemands"" ADD COLUMN IF NOT EXISTS ""Visibility"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""SourcingDemands"" ADD COLUMN IF NOT EXISTS ""InvitedMerchantIds"" uuid[] NOT NULL DEFAULT ARRAY[]::uuid[];

                -- 原定向单 → 邀标 + 收件人快照；原广播单保持默认（0=公开招标、空收件人）
                UPDATE ""SourcingDemands""
                SET ""Visibility"" = 1, ""InvitedMerchantIds"" = ARRAY[""TargetMerchantId""]
                WHERE ""TargetMerchantId"" IS NOT NULL;

                DROP INDEX IF EXISTS ""IX_SourcingDemands_TargetMerchantId"";
                ALTER TABLE ""SourcingDemands"" DROP COLUMN IF EXISTS ""TargetMerchantId"";
            ");

            // 收件人快照按"商户 ∈ 数组"查（商家侧"邀标给我的"），GIN 索引加速数组包含
            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_InvitedMerchantIds",
                table: "SourcingDemands",
                column: "InvitedMerchantIds")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourcingDemands_InvitedMerchantIds",
                table: "SourcingDemands");

            // 回滚：邀标单只还原"单收件人"的定向单（多收件人的邀标在旧模型不可表示，仅回到公开侧）
            migrationBuilder.Sql(@"
                ALTER TABLE ""SourcingDemands"" ADD COLUMN IF NOT EXISTS ""TargetMerchantId"" uuid;

                UPDATE ""SourcingDemands""
                SET ""TargetMerchantId"" = ""InvitedMerchantIds""[1]
                WHERE ""Visibility"" = 1 AND array_length(""InvitedMerchantIds"", 1) = 1;

                DROP INDEX IF EXISTS ""IX_SourcingDemands_TargetMerchantId"";
                CREATE INDEX ""IX_SourcingDemands_TargetMerchantId"" ON ""SourcingDemands"" (""TargetMerchantId"");

                ALTER TABLE ""SourcingDemands"" DROP COLUMN IF EXISTS ""Visibility"";
                ALTER TABLE ""SourcingDemands"" DROP COLUMN IF EXISTS ""InvitedMerchantIds"";
            ");
        }
    }
}
