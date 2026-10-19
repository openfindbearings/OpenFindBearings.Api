using MediatR;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.AdjustMerchantGrade
{
    /// <summary>
    /// 人工调整商家卡级处理器（v2.18.0）：校验"卡级不得凌驾于认证之上"后写入新卡级。
    /// 提交由 UnitOfWork 管道完成（ICommand），此处只做校验与变更标记。
    /// </summary>
    public class AdjustMerchantGradeCommandHandler : IRequestHandler<AdjustMerchantGradeCommand>
    {
        private readonly IMerchantRepository _merchants;

        /// <summary>构造：注入商家仓储</summary>
        public AdjustMerchantGradeCommandHandler(IMerchantRepository merchants)
        {
            _merchants = merchants;
        }

        /// <inheritdoc/>
        public async Task Handle(AdjustMerchantGradeCommand request, CancellationToken cancellationToken)
        {
            if (request.Grade == MerchantGrade.Unknown)
                throw new InvalidOperationException("卡级不能设为 Unknown");

            var merchant = await _merchants.GetByIdAsync(request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("商家不存在");

            // 认证是卡级之门：未认证一律注册商户，人工调级不得绕过认证（避免"未认证挂高卡"误导买家）
            if (request.Grade > MerchantGrade.Registered && !merchant.IsVerified)
                throw new InvalidOperationException("该商户未通过认证，不能授予卡级；请先完成认证");

            if (merchant.Grade == request.Grade)
                return;

            merchant.UpdateGrade(request.Grade);
            await _merchants.UpdateAsync(merchant, cancellationToken);
        }
    }
}
