using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // 改动说明（v1.38.0）：内置四角色赋中文显示名（Name 保持英文机器标识不动，
            // 鉴权 claims 比对不受影响）；幂等——只更新已存在的内置角色行
            migrationBuilder.Sql("""
                UPDATE "Roles" SET "DisplayName" = CASE "Name"
                    WHEN 'Admin' THEN '管理员'
                    WHEN 'Operator' THEN '操作员'
                    WHEN 'Auditor' THEN '审计员'
                    WHEN 'Individual' THEN '普通用户'
                END
                WHERE "Name" IN ('Admin','Operator','Auditor','Individual');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Roles");
        }
    }
}
