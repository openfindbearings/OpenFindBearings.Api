using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 系统配置配置类
    /// </summary>
    public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
    {
        public void Configure(EntityTypeBuilder<SystemConfig> builder)
        {
            builder.ToTable("SystemConfigs");

            builder.HasKey(sc => sc.Id);

            builder.Property(sc => sc.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(sc => sc.Key)
                .IsUnique();

            builder.Property(sc => sc.Value)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(sc => sc.Group)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(sc => sc.Description)
                .HasMaxLength(500);

            builder.Property(sc => sc.ValueType)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("string");

            builder.Property(sc => sc.IsSystem)
                .HasDefaultValue(false);

            builder.HasOne(sc => sc.Updater)
                .WithMany()
                .HasForeignKey(sc => sc.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(sc => sc.Group);
        }
    }
}
