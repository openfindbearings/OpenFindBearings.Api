using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InitiatorJoins",
                table: "StaffInvitations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "StaffInvitations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "StaffInvitations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Staff");

            migrationBuilder.CreateTable(
                name: "MerchantMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantMembers_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MerchantMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantMembers_MerchantId",
                table: "MerchantMembers",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantMembers_Status",
                table: "MerchantMembers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantMembers_User_Merchant",
                table: "MerchantMembers",
                columns: new[] { "UserId", "MerchantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MerchantMembers_UserId",
                table: "MerchantMembers",
                column: "UserId");

            // 存量数据回填：将 User.MerchantId 单值列迁移为 MerchantMember 成员行。
            // 映射规则（与设计文档一致）：持 MerchantAdmin 全局角色 -> MerchantAdmin；
            // 其余（含 MerchantStaff、无商户角色但绑定 MerchantId 的邀请路径）-> MerchantStaff。
            migrationBuilder.Sql("""
                INSERT INTO "MerchantMembers"
                    ("Id", "UserId", "MerchantId", "Role", "Status", "InvitedBy", "JoinedAt", "RemovedAt", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(),
                       u."Id",
                       u."MerchantId",
                       CASE WHEN EXISTS (
                           SELECT 1
                           FROM "UserRoles" ur
                           JOIN "Roles" r ON r."Id" = ur."RoleId"
                           WHERE ur."UserId" = u."Id" AND r."Name" = 'MerchantAdmin'
                       ) THEN 'MerchantAdmin' ELSE 'MerchantStaff' END,
                       'Active',
                       NULL,
                       COALESCE(u."UpdatedAt", NOW()),
                       NULL,
                       COALESCE(u."UpdatedAt", NOW()),
                       NULL,
                       true
                FROM "Users" u
                WHERE u."MerchantId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MerchantMembers");

            migrationBuilder.DropColumn(
                name: "InitiatorJoins",
                table: "StaffInvitations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StaffInvitations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "StaffInvitations");
        }
    }
}
