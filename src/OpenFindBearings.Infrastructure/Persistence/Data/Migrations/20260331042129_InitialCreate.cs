using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BearingTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BearingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    BusinessScope = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InvitationCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OperatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CompletedSub = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bearings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    InnerDiameter = table.Column<decimal>(type: "TEXT", precision: 10, scale: 3, nullable: false),
                    OuterDiameter = table.Column<decimal>(type: "TEXT", precision: 10, scale: 3, nullable: false),
                    Width = table.Column<decimal>(type: "TEXT", precision: 10, scale: 3, nullable: false),
                    Weight = table.Column<decimal>(type: "TEXT", precision: 10, scale: 3, nullable: true),
                    PrecisionGrade = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Material = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SealType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CageType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PerformanceHasData = table.Column<bool>(type: "INTEGER", nullable: true, defaultValue: false),
                    DynamicLoadRating = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    StaticLoadRating = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    LimitingSpeed = table.Column<decimal>(type: "TEXT", nullable: true),
                    OriginCountry = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 2),
                    BearingTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BrandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bearings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bearings_BearingTypes_BearingTypeId",
                        column: x => x.BearingTypeId,
                        principalTable: "BearingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bearings_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Avatar = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    GuestSessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMerged = table.Column<bool>(type: "INTEGER", nullable: false),
                    MergedToUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BearingInterchanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceBearingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetBearingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InterchangeType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, defaultValue: "exact"),
                    Confidence = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 80),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsBidirectional = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BearingInterchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BearingInterchanges_Bearings_SourceBearingId",
                        column: x => x.SourceBearingId,
                        principalTable: "Bearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BearingInterchanges_Bearings_TargetBearingId",
                        column: x => x.TargetBearingId,
                        principalTable: "Bearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MerchantBearings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BearingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PriceDescription = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PriceVisibility = table.Column<int>(type: "INTEGER", nullable: false),
                    NumericPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    StockDescription = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MinOrderDescription = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsOnSale = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsPendingApproval = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ApprovalComment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantBearings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantBearings_Bearings_BearingId",
                        column: x => x.BearingId,
                        principalTable: "Bearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MerchantBearings_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BeforeData = table.Column<string>(type: "jsonb", nullable: true),
                    AfterData = table.Column<string>(type: "jsonb", nullable: true),
                    OperatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorrectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BearingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SuggestedValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewComment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrectionRequests_Bearings_BearingId",
                        column: x => x.BearingId,
                        principalTable: "Bearings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CorrectionRequests_Bearings_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Bearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectionRequests_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CorrectionRequests_Merchants_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectionRequests_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectionRequests_Users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LicenseVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LicenseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewComment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicenseVerifications_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LicenseVerifications_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LicenseVerifications_Users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Group = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "string"),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemConfigs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserBearingFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BearingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBearingFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBearingFavorites_Bearings_BearingId",
                        column: x => x.BearingId,
                        principalTable: "Bearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBearingFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBearingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BearingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBearingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBearingHistories_Bearings_BearingId",
                        column: x => x.BearingId,
                        principalTable: "Bearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBearingHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMerchantFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMerchantFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMerchantFollows_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserMerchantFollows_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMerchantHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MerchantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMerchantHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMerchantHistories_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserMerchantHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OperatedAt",
                table: "AuditLogs",
                column: "OperatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OperatorId",
                table: "AuditLogs",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BearingInterchanges_Source_Target",
                table: "BearingInterchanges",
                columns: new[] { "SourceBearingId", "TargetBearingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BearingInterchanges_SourceBearingId",
                table: "BearingInterchanges",
                column: "SourceBearingId");

            migrationBuilder.CreateIndex(
                name: "IX_BearingInterchanges_TargetBearingId",
                table: "BearingInterchanges",
                column: "TargetBearingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_BearingTypeId",
                table: "Bearings",
                column: "BearingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_BrandId",
                table: "Bearings",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_InnerDiameter",
                table: "Bearings",
                column: "InnerDiameter");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_OuterDiameter",
                table: "Bearings",
                column: "OuterDiameter");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_PartNumber_BrandId",
                table: "Bearings",
                columns: new[] { "PartNumber", "BrandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_Width",
                table: "Bearings",
                column: "Width");

            migrationBuilder.CreateIndex(
                name: "IX_BearingTypes_Code",
                table: "BearingTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Code",
                table: "Brands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_BearingId",
                table: "CorrectionRequests",
                column: "BearingId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_MerchantId",
                table: "CorrectionRequests",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_ReviewedBy",
                table: "CorrectionRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_Status",
                table: "CorrectionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_SubmittedBy",
                table: "CorrectionRequests",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_TargetId",
                table: "CorrectionRequests",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_TargetType",
                table: "CorrectionRequests",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRequests_TargetType_TargetId",
                table: "CorrectionRequests",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseVerifications_MerchantId",
                table: "LicenseVerifications",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseVerifications_ReviewedBy",
                table: "LicenseVerifications",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseVerifications_Status",
                table: "LicenseVerifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseVerifications_SubmittedAt",
                table: "LicenseVerifications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseVerifications_SubmittedBy",
                table: "LicenseVerifications",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_BearingId",
                table: "MerchantBearings",
                column: "BearingId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_IsOnSale",
                table: "MerchantBearings",
                column: "IsOnSale");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_IsPendingApproval",
                table: "MerchantBearings",
                column: "IsPendingApproval");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_MerchantId",
                table: "MerchantBearings",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_MerchantId_BearingId",
                table: "MerchantBearings",
                columns: new[] { "MerchantId", "BearingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Grade",
                table: "Merchants",
                column: "Grade");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_IsVerified",
                table: "Merchants",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Name",
                table: "Merchants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Type",
                table: "Merchants",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_Email",
                table: "StaffInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_InvitationCode",
                table: "StaffInvitations",
                column: "InvitationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_IsCompleted",
                table: "StaffInvitations",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_MerchantId",
                table: "StaffInvitations",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_Phone",
                table: "StaffInvitations",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_Group",
                table: "SystemConfigs",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_Key",
                table: "SystemConfigs",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_UpdatedBy",
                table: "SystemConfigs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserBearingFavorites_BearingId",
                table: "UserBearingFavorites",
                column: "BearingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBearingFavorites_UserId",
                table: "UserBearingFavorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId_BearingId",
                table: "UserBearingFavorites",
                columns: new[] { "UserId", "BearingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBearingHistories_BearingId",
                table: "UserBearingHistories",
                column: "BearingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBearingHistories_UserId",
                table: "UserBearingHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBearingHistories_ViewedAt",
                table: "UserBearingHistories",
                column: "ViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_UserId_MerchantId",
                table: "UserMerchantFollows",
                columns: new[] { "UserId", "MerchantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMerchantFollows_MerchantId",
                table: "UserMerchantFollows",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMerchantFollows_UserId",
                table: "UserMerchantFollows",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMerchantHistories_MerchantId",
                table: "UserMerchantHistories",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMerchantHistories_UserId",
                table: "UserMerchantHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMerchantHistories_ViewedAt",
                table: "UserMerchantHistories",
                column: "ViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthUserId",
                table: "Users",
                column: "AuthUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GuestSessionId",
                table: "Users",
                column: "GuestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MerchantId",
                table: "Users",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserType",
                table: "Users",
                column: "UserType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BearingInterchanges");

            migrationBuilder.DropTable(
                name: "CorrectionRequests");

            migrationBuilder.DropTable(
                name: "LicenseVerifications");

            migrationBuilder.DropTable(
                name: "MerchantBearings");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "StaffInvitations");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "UserBearingFavorites");

            migrationBuilder.DropTable(
                name: "UserBearingHistories");

            migrationBuilder.DropTable(
                name: "UserMerchantFollows");

            migrationBuilder.DropTable(
                name: "UserMerchantHistories");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Bearings");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "BearingTypes");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Merchants");
        }
    }
}
