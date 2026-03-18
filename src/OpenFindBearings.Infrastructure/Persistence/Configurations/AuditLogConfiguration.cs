using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 审核日志配置类
    /// </summary>
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(al => al.Id);

            builder.Property(al => al.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(al => al.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(al => al.EntityId)
                .IsRequired();

            builder.Property(al => al.BeforeData)
                .HasColumnType("jsonb");

            builder.Property(al => al.AfterData)
                .HasColumnType("jsonb");

            builder.Property(al => al.Remarks)
                .HasMaxLength(500);

            builder.Property(al => al.OperatedAt)
                .IsRequired();

            builder.HasOne(al => al.Operator)
                .WithMany()
                .HasForeignKey(al => al.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // 索引
            builder.HasIndex(al => al.EntityType);
            builder.HasIndex(al => al.EntityId);
            builder.HasIndex(al => al.OperatorId);
            builder.HasIndex(al => al.OperatedAt);
            builder.HasIndex(al => new { al.EntityType, al.EntityId });
        }
    }
}
