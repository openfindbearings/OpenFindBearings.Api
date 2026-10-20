using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.AdjustMerchantGrade
{
    /// <summary>
    /// 人工调整商家牌级处理器（v2.18.0 引入，v2.19.0 有效期制适配）：校验"牌级不得凌驾于认证之上"后写入新牌级，
    /// 并为非普通档位重起有效期（人工调级不触发自动重算，避免立即被经营数据覆盖）。
    /// 提交由 UnitOfWork 管道完成（ICommand），此处只做校验与变更标记。
    /// </summary>
    public class AdjustMerchantGradeCommandHandler : IRequestHandler<AdjustMerchantGradeCommand>
    {
        private readonly IMerchantRepository _merchants;
        private readonly ISystemConfigRepository _configs;

        /// <summary>构造：注入商家仓储与系统配置（读取有效期天数）</summary>
        public AdjustMerchantGradeCommandHandler(IMerchantRepository merchants, ISystemConfigRepository configs)
        {
            _merchants = merchants;
            _configs = configs;
        }

        /// <inheritdoc/>
        public async Task Handle(AdjustMerchantGradeCommand request, CancellationToken cancellationToken)
        {
            if (request.Grade == MerchantGrade.Unknown)
                throw new InvalidOperationException("牌级不能设为 Unknown");

            var merchant = await _merchants.GetByIdAsync(request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("商家不存在");

            // 认证是卡级之门：未认证一律普通，人工调级不得绕过认证（避免"未认证挂牌级"误导买家）
            if (request.Grade > MerchantGrade.Ordinary && !merchant.IsVerified)
                throw new InvalidOperationException("该商户未通过认证，不能授予牌级；请先完成认证");

            if (merchant.Grade == request.Grade)
                return;

            merchant.UpdateGrade(request.Grade);

            // 改动说明（v2.19.0 有效期制）：人工调级后立即起期，页面才有"到期日"可显示；
            // 普通无牌不设有效期（UpdateGrade 已清空）
            if (request.Grade > MerchantGrade.Ordinary)
            {
                var periodDays = await _configs.GetValueAsync<int?>(MerchantGradeSettings.WindowDaysKey, null)
                    ?? MerchantGradeSettings.WindowDaysDefault;
                var now = DateTime.UtcNow;
                merchant.SetGradePeriod(now, now.AddDays(periodDays));
            }

            await _merchants.UpdateAsync(merchant, cancellationToken);
        }
    }
}
