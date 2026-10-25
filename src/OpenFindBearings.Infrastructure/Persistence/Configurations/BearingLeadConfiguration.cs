using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 型号线索实体配置
    /// 型号去重键建唯一索引，保证同一型号（忽略大小写）只累积一条线索
    /// </summary>
    public class BearingLeadConfiguration : IEntityTypeConfiguration<BearingLead>
    {
        /// <summary>
        /// 配置表名、字段长度、索引与唯一约束
        /// </summary>
        public void Configure(EntityTypeBuilder<BearingLead> builder)
        {
            builder.ToTable("BearingLeads");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.PartNumber)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.PartNumberKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.Brand)
                .HasMaxLength(100);

            builder.Property(l => l.Source)
                .IsRequired();

            builder.HasIndex(l => l.PartNumberKey)
                .IsUnique()
                .HasDatabaseName("UX_BearingLeads_PartNumberKey");

            builder.HasIndex(l => l.LastSeenAt);
            builder.HasIndex(l => l.OccurrenceCount);
        }
    }
}
