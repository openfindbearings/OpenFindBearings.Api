using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class MerchantDocumentConfiguration : IEntityTypeConfiguration<MerchantDocument>
    {
        public void Configure(EntityTypeBuilder<MerchantDocument> builder)
        {
            builder.ToTable("MerchantDocuments");

            builder.HasKey(lv => lv.Id);

            //// 商家关系（只配置一次）
            //builder.HasOne(lv => lv.Merchant)
            //    .WithMany()
            //    .HasForeignKey(lv => lv.MerchantId)
            //    .OnDelete(DeleteBehavior.Restrict);

            // 提交人关系
            builder.HasOne(lv => lv.Submitter)
                .WithMany()
                .HasForeignKey(lv => lv.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // 审核人关系
            builder.HasOne(lv => lv.Reviewer)
                .WithMany()
                .HasForeignKey(lv => lv.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Status 枚举配置
            builder.Property(lv => lv.Status)
                .HasConversion<int>()
                .HasDefaultValue(DocumentStatus.Pending)
                .IsRequired();

            // 改动说明（v2.7.0）：材料类型列。不设数据库默认值（枚举非零起点会触发 EF sentinel 告警），
            //   存量行由迁移 AddColumn 的一次性 defaultValue 回填为营业执照
            builder.Property(lv => lv.Type)
                .HasConversion<int>()
                .IsRequired();

            // 索引
            builder.HasIndex(lv => lv.MerchantId);
            builder.HasIndex(lv => lv.Status);
            builder.HasIndex(lv => lv.SubmittedAt);
            // v2.7.0 认证口径查询（某商户某类材料是否已批准）走商户+类型复合索引
            builder.HasIndex(lv => new { lv.MerchantId, lv.Type });
        }
    }
}
