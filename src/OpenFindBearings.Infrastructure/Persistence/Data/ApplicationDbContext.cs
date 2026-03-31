using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    public class ApplicationDbContext : DbContext
    {
        // ============ 核心业务 ============
        public DbSet<Bearing> Bearings { get; set; }
        public DbSet<BearingType> BearingTypes { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<MerchantBearing> MerchantBearings { get; set; }
        public DbSet<BearingInterchange> BearingInterchanges { get; set; }
        public DbSet<CorrectionRequest> CorrectionRequests { get; set; }
        public DbSet<LicenseVerification> LicenseVerifications { get; set; }

        // ============ 用户与权限 ============
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        // ============ 用户收藏与历史 ============
        public DbSet<UserBearingFavorite> UserFavorites { get; set; }
        public DbSet<UserMerchantFollow> UserFollows { get; set; }
        public DbSet<UserBearingHistory> UserBearingHistories { get; set; }
        public DbSet<UserMerchantHistory> UserMerchantHistories { get; set; }

        // ============ 审计与配置 ============
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        // ============ 邀请管理 ============
        public DbSet<StaffInvitation> StaffInvitations { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 应用所有配置
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 自动更新审计字段
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdateTimestamp();
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
