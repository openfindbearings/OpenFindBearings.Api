using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 寻货三表配置（需求/应答/应答型号行）：v1.35.0。
    /// 零内联依赖：不建到 User/Merchant/Bearing 的导航属性（只存 Id+索引），
    /// 保持寻货聚合"将来可整体拆出"的干净边界；应答唯一索引=一商户一需求单条应答。
    /// v1.5.0 多行标书：应答主表只存说明，报价/库存/交期下沉到行表 SourcingResponseItems
    /// </summary>
    public class SourcingConfiguration :
        IEntityTypeConfiguration<SourcingDemand>,
        IEntityTypeConfiguration<SourcingResponse>,
        IEntityTypeConfiguration<SourcingResponseItem>
    {
        /// <summary>
        /// 需求表：feed 查询主索引（状态+发布时间倒序）、发布人我的寻货索引、过期扫描索引
        /// </summary>
        public void Configure(EntityTypeBuilder<SourcingDemand> builder)
        {
            builder.ToTable("SourcingDemands");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.PublisherUserId).IsRequired();
            // 改动说明（v2.12.0 商户名义发布）：发布商户可空两列 + 商户维度索引（商家工作台查本店需求预留）
            builder.Property(d => d.PublisherMerchantId);
            builder.Property(d => d.PublisherMerchantName).HasMaxLength(100);
            builder.Property(d => d.PartNumber).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Brand).HasMaxLength(50);
            builder.Property(d => d.Quantity).HasMaxLength(30);
            builder.Property(d => d.ExpectedDelivery).HasMaxLength(30);
            builder.Property(d => d.Region).HasMaxLength(30);
            builder.Property(d => d.Description).HasMaxLength(500);
            builder.Property(d => d.Status).IsRequired();
            builder.Property(d => d.ExpiryAt).IsRequired();
            builder.Property(d => d.ResponseCount).IsRequired().HasDefaultValue(0);
            // 改动说明（v2.10.0 寻货置顶）：置顶到期时刻（UTC 可空），feed 置顶排前依据
            builder.Property(d => d.PinnedUntil);
            // 改动说明（v2.12.0 列表删除）：软删标记（仅发布方列表隐藏，数据与应答方视图保留）
            builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);
            // 改动说明（v1.8.0 公开/邀请）：曝光范围（0=公开为默认，存量与新建均落公开侧）
            builder.Property(d => d.Visibility).IsRequired().HasDefaultValue(SourcingVisibility.Public);
            // 改动说明（v1.8.0 邀请）：收件人快照以 uuid[] 一列承载（无外键、无逐收件人状态，只需 ANY 判定）
            builder.Property(d => d.InvitedMerchantIds).IsRequired();
            // 改动说明（v1.8.2 彻底幂等）：发布幂等键（可空），配合下方过滤唯一索引实现"重放返回原单"
            builder.Property(d => d.IdempotencyKey).HasMaxLength(100);

            // feed：进行中列表按时间倒序是最高频查询
            builder.HasIndex(d => new { d.Status, d.CreatedAt })
                .HasDatabaseName("IX_SourcingDemands_Status_CreatedAt");
            // 我的寻货（我发布的）
            builder.HasIndex(d => d.PublisherUserId)
                .HasDatabaseName("IX_SourcingDemands_PublisherUserId");
            // 商户名义发布的单按商户查（v2.12.0）
            builder.HasIndex(d => d.PublisherMerchantId)
                .HasDatabaseName("IX_SourcingDemands_PublisherMerchantId");
            // 改动说明（v1.8.0 邀请）：收件人快照按"商户 ∈ 数组"查（商家侧"点名邀请我的"），GIN 索引加速数组包含
            builder.HasIndex(d => d.InvitedMerchantIds)
                .HasDatabaseName("IX_SourcingDemands_InvitedMerchantIds")
                .HasMethod("gin");
            // 型号搜索（自由文本模糊匹配前缀加速）
            builder.HasIndex(d => d.PartNumber)
                .HasDatabaseName("IX_SourcingDemands_PartNumber");
            // 改动说明（v1.8.2 彻底幂等）：发布幂等键的过滤唯一索引——同发布人同 key 只允许一张单，
            // 键为空（未提供幂等键）的行不参与唯一约束（过滤条件 IS NOT NULL）
            builder.HasIndex(d => new { d.PublisherUserId, d.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL")
                .HasDatabaseName("UX_SourcingDemands_Publisher_IdemKey");
        }

        /// <summary>
        /// 应答表：(DemandId, MerchantId) 唯一=一商户一需求只一条应答（可更新）；
        /// MerchantId 索引支撑商家"我的应答"列表；
        /// v1.5.0 报价/库存/交期列已下沉到行表，主表只留说明 Remark
        /// </summary>
        public void Configure(EntityTypeBuilder<SourcingResponse> builder)
        {
            builder.ToTable("SourcingResponses");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.DemandId).IsRequired();
            builder.Property(r => r.MerchantId).IsRequired();
            builder.Property(r => r.RespondedUserId).IsRequired();
            builder.Property(r => r.Remark).IsRequired().HasMaxLength(300);
            builder.Property(r => r.Status).IsRequired();

            // 应答型号行（级联删除：应答删除时行随删）
            builder.HasMany(r => r.Items)
                .WithOne()
                .HasForeignKey(i => i.ResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.DemandId, r.MerchantId })
                .IsUnique()
                .HasDatabaseName("UX_SourcingResponses_Demand_Merchant");
            builder.HasIndex(r => r.MerchantId)
                .HasDatabaseName("IX_SourcingResponses_MerchantId");
        }

        /// <summary>
        /// 应答型号行表：按应答检索（比价视图逐应答拉行）；每行型号必填，报价/库存/交期可选
        /// </summary>
        public void Configure(EntityTypeBuilder<SourcingResponseItem> builder)
        {
            builder.ToTable("SourcingResponseItems");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.ResponseId).IsRequired();
            builder.Property(i => i.PartNumber).IsRequired().HasMaxLength(100);
            builder.Property(i => i.BearingId);
            builder.Property(i => i.Price).HasPrecision(12, 2);
            builder.Property(i => i.Stock).HasMaxLength(50);
            builder.Property(i => i.LeadTime).HasMaxLength(50);

            builder.HasIndex(i => i.ResponseId)
                .HasDatabaseName("IX_SourcingResponseItems_ResponseId");
        }
    }
}
