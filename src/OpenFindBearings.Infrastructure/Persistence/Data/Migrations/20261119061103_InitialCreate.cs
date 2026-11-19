using System;
using System.Collections.Generic;
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
                name: "AchievementDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImageKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProgressTarget = table.Column<int>(type: "integer", nullable: false),
                    MetaPoints = table.Column<int>(type: "integer", nullable: false),
                    TitleReward = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Rare = table.Column<bool>(type: "boolean", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsLimited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LimitedOrdinal = table.Column<int>(type: "integer", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "ApiCallLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    ApiPath = table.Column<string>(type: "text", nullable: false),
                    HttpMethod = table.Column<string>(type: "text", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    ClientIp = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiCallLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BearingLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PartNumberKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsIgnored = table.Column<bool>(type: "boolean", nullable: false),
                    IgnoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IgnoredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BearingLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BearingTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EnglishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StructureFeatures = table.Column<string>(type: "text", nullable: true),
                    LoadCharacteristics = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TypicalPrefixes = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MainApplications = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSeparable = table.Column<bool>(type: "boolean", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DataSourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BearingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FoundedYear = table.Column<int>(type: "integer", nullable: true),
                    ParentCompany = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Advantages = table.Column<string>(type: "text", nullable: true),
                    SuffixNotes = table.Column<string>(type: "text", nullable: true),
                    MainApplications = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    DataSourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Icon = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    PointPrice = table.Column<int>(type: "integer", nullable: false),
                    FlashPrice = table.Column<int>(type: "integer", nullable: true),
                    FlashStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlashEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationHours = table.Column<int>(type: "integer", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    SoldCount = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OwnerMerchantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetKind = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AuditState = table.Column<int>(type: "integer", nullable: false),
                    AuditRemark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PointsSpent = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetRef = table.Column<Guid>(type: "uuid", nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShipStatus = table.Column<int>(type: "integer", nullable: false),
                    ReceiverName = table.Column<string>(type: "text", nullable: true),
                    ReceiverPhone = table.Column<string>(type: "text", nullable: true),
                    ReceiverAddress = table.Column<string>(type: "text", nullable: true),
                    ShipTracking = table.Column<string>(type: "text", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantPointAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantPointAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantPointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    GrantType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    BizId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantPointTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EnglishName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UnifiedSocialCreditCode = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Contact_QQ = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BusinessScope = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VerifyRequested = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OnboardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Grade = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    GradeGraceUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradePeriodStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradePeriodEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    SuspensionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApplicationMode = table.Column<int>(type: "integer", nullable: false),
                    ProductCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FollowerCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DataSourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDataVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DataRemark = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantTaskCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MetricValue = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantTaskCompletions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantTaskDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetValue = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    RewardAmount = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantTaskDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BizType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BizId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    LastCheckinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveCheckinDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointGrantRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false),
                    LadderJson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DoubleChance = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LegendChance = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointGrantRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    MinTotalEarned = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LevelUpBonus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointRewardClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BizKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    GrantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstClaimerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointRewardClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    GrantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    BizId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExpireAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

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
                name: "SourcingDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherMerchantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublisherMerchantName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    InvitedMerchantIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ExpectedDelivery = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Region = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelectedResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactUnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PinnedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingDemands", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "SourcingResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdoptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FeedbackOutcome = table.Column<int>(type: "integer", nullable: true),
                    FeedbackReason = table.Column<string>(type: "text", nullable: true),
                    FeedbackAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CountedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCounted = table.Column<bool>(type: "boolean", nullable: false),
                    FlagNote = table.Column<string>(type: "text", nullable: true),
                    FeedbackIp = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvitationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Staff"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    InitiatorJoins = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedSub = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserBehaviorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Keyword = table.Column<string>(type: "text", nullable: true),
                    ClientIp = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ExtraData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ImageUrls = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[0]),
                    Contact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminReply = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RepliedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GuestSessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SubscriptionExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegistrationSource = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    RegisterIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Occupation = table.Column<int>(type: "integer", nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EquippedTitle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SearchCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    QueryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FirstSearchAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSearchAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsGuest = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsMerged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MergedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsAnonymized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bearings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OldNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CodeSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BearingType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StructureType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SizeSeries = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsStandard = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InnerDiameter = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    OuterDiameter = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Width = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    ChamferRmin = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    ChamferRmax = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    PrecisionGrade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Material = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SealType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PerformanceHasData = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    Performance_DynamicLoad = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Performance_StaticLoad = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    LimitingSpeed = table.Column<int>(type: "integer", nullable: true),
                    LimitingSpeedGrease = table.Column<int>(type: "integer", nullable: true),
                    LimitingSpeedOil = table.Column<int>(type: "integer", nullable: true),
                    OriginCountry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Trademark = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    BearingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DataRemark = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Image3DUrl = table.Column<string>(type: "text", nullable: true),
                    Image2DUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "SourcingResponseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Stock = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeadTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingResponseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourcingResponseItems_SourcingResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "SourcingResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeforeData = table.Column<string>(type: "jsonb", nullable: true),
                    AfterData = table.Column<string>(type: "jsonb", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "MerchantDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantDocuments_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MerchantDocuments_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MerchantDocuments_Users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNo = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    Plan = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "string"),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "UserMerchantFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredBrandIds = table.Column<string>(type: "text", nullable: true),
                    PreferredBearingTypeIds = table.Column<string>(type: "text", nullable: true),
                    PriceRangePreference = table.Column<string>(type: "text", nullable: true),
                    PreferredProvince = table.Column<string>(type: "text", nullable: true),
                    PreferredCity = table.Column<string>(type: "text", nullable: true),
                    EmailNotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsNotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WeChatNotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "BearingInterchanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceBearingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetBearingId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterchangeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "exact"),
                    Confidence = table.Column<int>(type: "integer", nullable: false, defaultValue: 80),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsBidirectional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataSourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "CorrectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SuggestedValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "MerchantBearings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PriceVisibility = table.Column<int>(type: "integer", nullable: false),
                    NumericPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    StockDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinOrderDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SaleState = table.Column<int>(type: "integer", nullable: false),
                    RestockEta = table.Column<string>(type: "text", nullable: true),
                    PinnedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovalComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataSourceType = table.Column<int>(type: "integer", nullable: true),
                    ImageUrls = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[0]),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "UserBearingFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "IX_BearingLeads_LastSeenAt",
                table: "BearingLeads",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_BearingLeads_OccurrenceCount",
                table: "BearingLeads",
                column: "OccurrenceCount");

            migrationBuilder.CreateIndex(
                name: "UX_BearingLeads_PartNumberKey",
                table: "BearingLeads",
                column: "PartNumberKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_BearingType",
                table: "Bearings",
                column: "BearingType");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_BearingTypeId",
                table: "Bearings",
                column: "BearingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_BrandId",
                table: "Bearings",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_Category",
                table: "Bearings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_Code_Type",
                table: "Bearings",
                columns: new[] { "PartNumber", "BearingType" });

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_CurrentCode_BrandId",
                table: "Bearings",
                columns: new[] { "PartNumber", "BrandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_InnerDiameter",
                table: "Bearings",
                column: "InnerDiameter");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_IsStandard",
                table: "Bearings",
                column: "IsStandard");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_IsVerified",
                table: "Bearings",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_OuterDiameter",
                table: "Bearings",
                column: "OuterDiameter");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_SizeSeries",
                table: "Bearings",
                column: "SizeSeries");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_StructureType",
                table: "Bearings",
                column: "StructureType");

            migrationBuilder.CreateIndex(
                name: "IX_Bearings_Type_IsStandard",
                table: "Bearings",
                columns: new[] { "BearingType", "IsStandard" });

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
                name: "IX_MallItems_Owner_Audit",
                table: "MallItems",
                columns: new[] { "OwnerMerchantId", "AuditState" });

            migrationBuilder.CreateIndex(
                name: "UX_MallItems_Key",
                table: "MallItems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_User_Created",
                table: "MallOrders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_BearingId",
                table: "MerchantBearings",
                column: "BearingId");

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
                name: "IX_MerchantBearings_ReviewStatus",
                table: "MerchantBearings",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_SaleState",
                table: "MerchantBearings",
                column: "SaleState");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_MerchantId",
                table: "MerchantDocuments",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_MerchantId_Type",
                table: "MerchantDocuments",
                columns: new[] { "MerchantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_ReviewedBy",
                table: "MerchantDocuments",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_Status",
                table: "MerchantDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_SubmittedAt",
                table: "MerchantDocuments",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_SubmittedBy",
                table: "MerchantDocuments",
                column: "SubmittedBy");

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

            migrationBuilder.CreateIndex(
                name: "UX_MerchantPointAccounts_MerchantId",
                table: "MerchantPointAccounts",
                column: "MerchantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MerchantPointTx_Merchant_Created",
                table: "MerchantPointTransactions",
                columns: new[] { "MerchantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantPointTx_Merchant_Type_Created",
                table: "MerchantPointTransactions",
                columns: new[] { "MerchantId", "GrantType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_MerchantPointTransactions_BizId",
                table: "MerchantPointTransactions",
                column: "BizId",
                unique: true,
                filter: "\"BizId\" IS NOT NULL");

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
                name: "IX_Merchants_Status",
                table: "Merchants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Type",
                table: "Merchants",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_UnifiedSocialCreditCode",
                table: "Merchants",
                column: "UnifiedSocialCreditCode");

            migrationBuilder.CreateIndex(
                name: "UX_MerchantTaskCompletions_Task_Merchant_Period",
                table: "MerchantTaskCompletions",
                columns: new[] { "TaskKey", "MerchantId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MerchantTaskDefinitions_Key",
                table: "MerchantTaskDefinitions",
                column: "TaskKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_UserId",
                table: "PaymentRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointAccounts_UserId",
                table: "PointAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PointGrantRules_GrantType",
                table: "PointGrantRules",
                column: "GrantType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PointLevels_Level",
                table: "PointLevels",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PointRewardClaims_BizKey",
                table: "PointRewardClaims",
                column: "BizKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserId_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserId_GrantType_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "UserId", "GrantType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_PointTransactions_BizId",
                table: "PointTransactions",
                column: "BizId",
                unique: true,
                filter: "\"BizId\" IS NOT NULL");

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
                name: "IX_SourcingDemands_InvitedMerchantIds",
                table: "SourcingDemands",
                column: "InvitedMerchantIds")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_PartNumber",
                table: "SourcingDemands",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_PublisherMerchantId",
                table: "SourcingDemands",
                column: "PublisherMerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_PublisherUserId",
                table: "SourcingDemands",
                column: "PublisherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_Status_CreatedAt",
                table: "SourcingDemands",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_SourcingDemands_Publisher_IdemKey",
                table: "SourcingDemands",
                columns: new[] { "PublisherUserId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingMessages_Conversation_CreatedAt",
                table: "SourcingMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SourcingResponseItems_ResponseId",
                table: "SourcingResponseItems",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingResponses_MerchantId",
                table: "SourcingResponses",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "UX_SourcingResponses_Demand_Merchant",
                table: "SourcingResponses",
                columns: new[] { "DemandId", "MerchantId" },
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
                name: "IX_UserFeedbacks_Status_CreatedAt",
                table: "UserFeedbacks",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_UserId_CreatedAt",
                table: "UserFeedbacks",
                columns: new[] { "UserId", "CreatedAt" });

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
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId");

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
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsGuest",
                table: "Users",
                column: "IsGuest");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsGuest_IsActive",
                table: "Users",
                columns: new[] { "IsGuest", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastActiveAt",
                table: "Users",
                column: "LastActiveAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Level",
                table: "Users",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Level_IsActive",
                table: "Users",
                columns: new[] { "Level", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RegistrationSource",
                table: "Users",
                column: "RegistrationSource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementDefinitions");

            migrationBuilder.DropTable(
                name: "AchievementUnlocks");

            migrationBuilder.DropTable(
                name: "ApiCallLogs");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BearingInterchanges");

            migrationBuilder.DropTable(
                name: "BearingLeads");

            migrationBuilder.DropTable(
                name: "CorrectionRequests");

            migrationBuilder.DropTable(
                name: "MallItems");

            migrationBuilder.DropTable(
                name: "MallOrders");

            migrationBuilder.DropTable(
                name: "MerchantBearings");

            migrationBuilder.DropTable(
                name: "MerchantDocuments");

            migrationBuilder.DropTable(
                name: "MerchantMembers");

            migrationBuilder.DropTable(
                name: "MerchantPointAccounts");

            migrationBuilder.DropTable(
                name: "MerchantPointTransactions");

            migrationBuilder.DropTable(
                name: "MerchantTaskCompletions");

            migrationBuilder.DropTable(
                name: "MerchantTaskDefinitions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "PointAccounts");

            migrationBuilder.DropTable(
                name: "PointGrantRules");

            migrationBuilder.DropTable(
                name: "PointLevels");

            migrationBuilder.DropTable(
                name: "PointRewardClaims");

            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SourcingConversations");

            migrationBuilder.DropTable(
                name: "SourcingDemands");

            migrationBuilder.DropTable(
                name: "SourcingMessages");

            migrationBuilder.DropTable(
                name: "SourcingResponseItems");

            migrationBuilder.DropTable(
                name: "StaffInvitations");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "UserBearingFavorites");

            migrationBuilder.DropTable(
                name: "UserBearingHistories");

            migrationBuilder.DropTable(
                name: "UserBehaviorLogs");

            migrationBuilder.DropTable(
                name: "UserFeedbacks");

            migrationBuilder.DropTable(
                name: "UserMerchantFollows");

            migrationBuilder.DropTable(
                name: "UserMerchantHistories");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SourcingResponses");

            migrationBuilder.DropTable(
                name: "Bearings");

            migrationBuilder.DropTable(
                name: "Merchants");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "BearingTypes");

            migrationBuilder.DropTable(
                name: "Brands");
        }
    }
}
