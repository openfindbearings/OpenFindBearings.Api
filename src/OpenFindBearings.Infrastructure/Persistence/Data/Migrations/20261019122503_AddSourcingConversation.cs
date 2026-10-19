using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcingConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourcingConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherMerchantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublisherLastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MerchantLastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastMessagePreview = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourcingMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderSide = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourcingConversations_Merchant_LastAt",
                table: "SourcingConversations",
                columns: new[] { "MerchantId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SourcingConversations_Publisher_LastAt",
                table: "SourcingConversations",
                columns: new[] { "PublisherUserId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "UX_SourcingConversations_Demand_Merchant",
                table: "SourcingConversations",
                columns: new[] { "DemandId", "MerchantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourcingMessages_Conversation_CreatedAt",
                table: "SourcingMessages",
                columns: new[] { "ConversationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourcingConversations");

            migrationBuilder.DropTable(
                name: "SourcingMessages");
        }
    }
}
