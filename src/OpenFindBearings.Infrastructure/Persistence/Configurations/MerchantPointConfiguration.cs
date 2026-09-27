using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 商家金库配置（v2.4.0 工会经济）：与个人积分同构——
    /// 账户商户维度唯一 + xmin 并发令牌（结算/消费高频后不丢更新）；
    /// 流水 BizId 部分唯一索引做幂等（trickle/settle/burn 三类键）
    /// </summary>
    public class MerchantPointConfiguration :
        IEntityTypeConfiguration<MerchantPointAccount>,
        IEntityTypeConfiguration<MerchantPointTransaction>
    {
        /// <summary>配置金库账户（一商一户 + 并发令牌）</summary>
        public void Configure(EntityTypeBuilder<MerchantPointAccount> builder)
        {
            builder.ToTable("MerchantPointAccounts");
            builder.HasKey(a => a.Id);

            builder.HasIndex(a => a.MerchantId)
                .IsUnique()
                .HasDatabaseName("UX_MerchantPointAccounts_MerchantId");

            // 与 PointAccounts 同款：映射 PG 系统列 xmin 为并发令牌（纯模型零 DDL），
            // 挂礼结算与金库消费可能并发，防止"读-改-写"丢更新
            builder.Property<uint>("xmin").HasColumnType("xid").IsRowVersion().IsConcurrencyToken();
        }

        /// <summary>配置金库流水（幂等键 + 上限汇总索引 + 明细分页索引）</summary>
        public void Configure(EntityTypeBuilder<MerchantPointTransaction> builder)
        {
            builder.ToTable("MerchantPointTransactions");
            builder.HasKey(t => t.Id);

            builder.HasIndex(t => t.BizId)
                .IsUnique()
                .HasFilter("\"BizId\" IS NOT NULL")
                .HasDatabaseName("UX_MerchantPointTransactions_BizId");

            // 日/月上限汇总主路径（商户+场景+时间）与明细分页
            builder.HasIndex(t => new { t.MerchantId, t.GrantType, t.CreatedAt })
                .HasDatabaseName("IX_MerchantPointTx_Merchant_Type_Created");
            builder.HasIndex(t => new { t.MerchantId, t.CreatedAt })
                .HasDatabaseName("IX_MerchantPointTx_Merchant_Created");

            builder.Property(t => t.GrantType).IsRequired().HasMaxLength(64);
            builder.Property(t => t.BizId).HasMaxLength(160);
            builder.Property(t => t.Remark).HasMaxLength(256);
        }
    }
}
