using System.Text.Json;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 轴承连连看 Provider（v2.10.1 游戏中心首款插件游戏）：
    /// 题库=MinIO 爬取图管线沉淀的有图轴承（类型打散防"照片脸盲"），
    /// 结算=每局 gameId 幂等发分（规则 minigame：5 分/局、日上限 10 分）。
    /// 原型口径：低额奖励客户端信任制（与签到同级防刷），不验游戏过程。
    /// </summary>
    public class LinkupGameProvider : IGameProvider
    {
        /// <summary>游戏键常量（Taro 注册表与 BFF 路径共用口径）</summary>
        public const string GameKey = "linkup";

        private readonly IBearingRepository _bearings;
        private readonly IPointsService _points;

        public LinkupGameProvider(IBearingRepository bearings, IPointsService points)
        {
            _bearings = bearings;
            _points = points;
        }

        /// <inheritdoc/>
        public string Key => GameKey;

        /// <inheritdoc/>
        public string DisplayName => "轴承连连看";

        /// <inheritdoc/>
        public async Task<object?> GetBoardAsync(Guid userId, int? size, CancellationToken cancellationToken = default)
        {
            // pairs 夹在 6~24：防超大棋盘拉库/渲染爆炸（默认 18 对 = 6x6）
            var n = Math.Clamp(size ?? 18, 6, 24);
            var bearings = await _bearings.GetRandomWithImageAsync(n, cancellationToken);
            return bearings.Select(b => new
            {
                id = b.Id,
                partNumber = b.PartNumber,
                imageUrl = b.Image2DUrl ?? b.Image3DUrl
            });
        }

        /// <inheritdoc/>
        public async Task<int> ReportResultAsync(Guid userId, JsonElement payload, CancellationToken cancellationToken = default)
        {
            var gameId = payload.TryGetProperty("gameId", out var g) ? g.GetString() : null;
            if (string.IsNullOrWhiteSpace(gameId) || gameId.Length > 64)
                return 0;

            return await _points.GrantAsync(
                userId,
                PointTransaction.TypeMinigame,
                bizId: $"minigame:{gameId}",
                remark: "轴承连连看胜利",
                cancellationToken: cancellationToken);
        }
    }
}
