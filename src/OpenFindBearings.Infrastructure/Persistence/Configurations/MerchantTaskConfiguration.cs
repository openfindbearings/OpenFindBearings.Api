using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 商家集体任务配置（v2.6.0）：TaskKey 唯一（奖励 bizId 锚点）；
    /// 完成台账 (TaskKey, MerchantId, PeriodKey) 唯一=周期防重铁闸（结算并发下靠它兜底）
    /// </summary>
    public class MerchantTaskConfiguration :
        IEntityTypeConfiguration<MerchantTaskDefinition>,
        IEntityTypeConfiguration<MerchantTaskCompletion>
    {
        /// <summary>配置任务定义表</summary>
        public void Configure(EntityTypeBuilder<MerchantTaskDefinition> builder)
        {
            builder.ToTable("MerchantTaskDefinitions");
            builder.HasKey(t => t.Id);

            builder.HasIndex(t => t.TaskKey)
                .IsUnique()
                .HasDatabaseName("UX_MerchantTaskDefinitions_Key");

            builder.Property(t => t.TaskKey).IsRequired().HasMaxLength(64);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(64);
            builder.Property(t => t.Description).IsRequired().HasMaxLength(256);
            builder.Property(t => t.MetricKey).IsRequired().HasMaxLength(32);
        }

        /// <summary>配置完成台账表（周期唯一键）</summary>
        public void Configure(EntityTypeBuilder<MerchantTaskCompletion> builder)
        {
            builder.ToTable("MerchantTaskCompletions");
            builder.HasKey(c => c.Id);

            builder.HasIndex(c => new { c.TaskKey, c.MerchantId, c.PeriodKey })
                .IsUnique()
                .HasDatabaseName("UX_MerchantTaskCompletions_Task_Merchant_Period");

            builder.Property(c => c.TaskKey).IsRequired().HasMaxLength(64);
            builder.Property(c => c.PeriodKey).IsRequired().HasMaxLength(16);
        }
    }
}
