using MediatR;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMallItem
{
    /// <summary>
    /// 商城商品详情处理器（v1.59.16）：校验公域可见性由仓储完成，
    /// 命中后套用与货源流相同的 DTO 投影，再补商家摘要与型号/品牌描述。
    /// </summary>
    public class GetMallItemQueryHandler : IRequestHandler<GetMallItemQuery, MallItemDetailDto?>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;

        public GetMallItemQueryHandler(IMerchantBearingRepository merchantBearingRepository)
        {
            _merchantBearingRepository = merchantBearingRepository;
        }

        /// <summary>查询单条商城商品详情；不可见时返回 null</summary>
        public async Task<MallItemDetailDto?> Handle(GetMallItemQuery request, CancellationToken cancellationToken)
        {
            var item = await _merchantBearingRepository.GetMallItemAsync(request.Id, cancellationToken);
            if (item?.Merchant is null)
            {
                return null;
            }

            var merchant = item.Merchant;
            return new MallItemDetailDto(
                item.ToDto(),
                new MallItemMerchantDto(
                    merchant.Id,
                    merchant.Name,
                    merchant.LogoUrl,
                    merchant.Grade.ToString(),
                    merchant.IsVerified,
                    merchant.Contact?.Phone ?? merchant.Contact?.Mobile),
                item.Bearing?.Description,
                item.Bearing?.Brand?.Description);
        }
    }
}