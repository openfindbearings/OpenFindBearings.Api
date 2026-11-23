using MediatR;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Queries.Bearings.GetBearingQuery;
using OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantsByBearing;
using OpenFindBearings.Application.Queries.Merchants.GetMerchant;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;
using QRCoder;
using SkiaSharp;

namespace OpenFindBearings.Api.Endpoints;

/// <summary>
/// 分享素材端点（扫码与分享需求 v0.1.0）：二维码与海报属于平台素材，
/// 由 API 统一生成并缓存到对象存储，BFF 只做转发与落地页渲染。
/// 码内只编码落地页链接（/s/{type}/{id}），不编码业务数据。
/// </summary>
public static class ShareEndpoints
{
    /// <summary>海报画布尺寸（竖版，适合手机预览与打印）。</summary>
    private const int PosterWidth = 1080;
    private const int PosterHeight = 1440;

    /// <summary>平台名兜底值：海报落款，优先读系统配置 Site.PlatformName</summary>
    private const string DefaultPlatformName = "OpenFindBearings";

    /// <summary>
    /// 注册分享素材端点：GET /api/share/qr 与 GET /api/share/poster（均匿名）。
    /// </summary>
    public static void MapShareEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/share");

        // 二维码：轻量、确定性输出，每次现算（毫秒级），不落存储
        group.MapGet("/qr", (string type, Guid id, string? src, IConfiguration config) =>
        {
            var normalizedType = NormalizeType(type);
            if (normalizedType is null)
            {
                return Results.BadRequest(new { message = "type 仅支持 bearing 或 merchant" });
            }

            var source = NormalizeSource(src) ?? "qr";
            var payload = LandingUrl(config, normalizedType, id, source);
            var png = RenderQrPng(payload);
            return Results.File(png, "image/png");
        })
        .WithName("ShareQrCode")
        .WithSummary("分享二维码（指向 H5 落地页）")
        .AllowAnonymous();

        // 海报：合成一次后落对象存储，后续命中缓存直接返回字节
        group.MapGet("/poster", async (
            string type,
            Guid id,
            string? src,
            IMediator mediator,
            IObjectStorageService storage,
            IConfiguration config,
            ISystemConfigRepository configs,
            CancellationToken ct) =>
        {
            var normalizedType = NormalizeType(type);
            if (normalizedType is null)
            {
                return Results.BadRequest(new { message = "type 仅支持 bearing 或 merchant" });
            }

            var source = NormalizeSource(src) ?? "poster";
            // 改动说明（v1.59.17）：缓存键不再带 src——来源只用于统计，不影响图片内容；
            // 带 src 会让同一对象从 App/二维码/海报不同入口各存一份，白占存储。
            var key = $"uploads/share/posters/{normalizedType}/{id}.png";
            if (await storage.ExistsAsync(key, ct))
            {
                var cached = await storage.DownloadAsync(key, ct);
                if (cached is not null)
                {
                    return Results.File(cached, "image/png");
                }
            }

            var payload = LandingUrl(config, normalizedType, id, source);
            var qrPng = RenderQrPng(payload, 10);
            // 改动说明（v1.59.21）：平台名改读系统配置 Site.PlatformName（Admin 系统配置页可改），代码只留兜底值
            var platformName = (await configs.GetValueAsync<string>("Site.PlatformName", null))?.Trim();
            if (string.IsNullOrWhiteSpace(platformName)) platformName = DefaultPlatformName;
            var png = normalizedType == "merchant"
                ? await RenderMerchantPosterAsync(id, qrPng, platformName, mediator, storage, ct)
                : await RenderBearingPosterAsync(id, qrPng, platformName, mediator, storage, ct);

            if (png is null)
            {
                return Results.NotFound();
            }

            await storage.UploadAsync(key, png, "image/png", ct);
            return Results.File(png, "image/png");
        })
        .WithName("SharePoster")
        .WithSummary("分享海报（后端合成并缓存到对象存储）")
        .AllowAnonymous();
    }

    /// <summary>校验并归一化码类型；非法返回 null。</summary>
    private static string? NormalizeType(string? type)
    {
        var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "bearing" or "merchant" ? normalized : null;
    }

    /// <summary>分享来源白名单（app / h5 / weapp / qr / poster），非法值视为未提供。</summary>
    private static readonly HashSet<string> ShareSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "app", "h5", "weapp", "qr", "poster"
    };

    /// <summary>归一化分享来源：命中白名单返回小写值，否则返回 null。</summary>
    private static string? NormalizeSource(string? src)
    {
        if (string.IsNullOrWhiteSpace(src)) return null;
        var trimmed = src.Trim();
        return ShareSources.Contains(trimmed) ? trimmed.ToLowerInvariant() : null;
    }

    /// <summary>
    /// 拼接落地页链接：轴承带货源视角参数，商家为店铺主页；H5 base 取 Share:H5BaseUrl。
    /// </summary>
    private static string LandingUrl(IConfiguration config, string type, Guid id, string source)
    {
        var h5Base = (config["Share:H5BaseUrl"] ?? string.Empty).TrimEnd('/');
        return type == "bearing"
            ? $"{h5Base}/s/bearing/{id}?view=supply&src={source}"
            : $"{h5Base}/s/merchant/{id}?src={source}";
    }

    /// <summary>把分享链接渲染为二维码 PNG（容错级别 Q）。</summary>
    private static byte[] RenderQrPng(string payload, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// 合成商家店铺海报：Logo + 名称 + 认证标识 + 牌级 + 在售与粉丝数 + 二维码。
    /// </summary>
    private static async Task<byte[]?> RenderMerchantPosterAsync(
        Guid id,
        byte[] qrPng,
        string platformName,
        IMediator mediator,
        IObjectStorageService storage,
        CancellationToken ct)
    {
        var detail = await mediator.Send(new GetMerchantQuery { Id = id, IsAuthenticated = false }, ct);
        if (detail is null) return null;

        using var surface = SKSurface.Create(new SKImageInfo(PosterWidth, PosterHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using (var bar = new SKPaint { Color = new SKColor(0x25, 0x63, 0xEB), IsAntialias = true })
        {
            canvas.DrawRect(0, 0, PosterWidth, 14, bar);
        }

        var logoBitmap = await TryLoadBitmapAsync(storage, detail.LogoUrl, ct);
        DrawLogo(canvas, logoBitmap, detail.Name, 200, 140);

        var bold = ResolveTypeface(true);
        var regular = ResolveTypeface(false);
        var primary = new SKColor(0x0F, 0x17, 0x2A);
        var secondary = new SKColor(0x47, 0x55, 0x69);
        var tertiary = new SKColor(0x64, 0x74, 0x8B);

        // 改动说明（v1.4.32）：logo 与名称间距偏小，整体下移 30px 并把后续元素同步下移
        DrawCenteredText(canvas, detail.Name, bold, 60, primary, 410, 900);
        if (!string.IsNullOrWhiteSpace(detail.CompanyName) && detail.CompanyName != detail.Name)
        {
            DrawCenteredText(canvas, detail.CompanyName!, regular, 32, secondary, 470, 900);
        }

        DrawBadge(canvas, detail.IsVerified ? "认证商家" : "轴承商家", regular, 550);

        if (!string.IsNullOrWhiteSpace(detail.GradeDisplay))
        {
            DrawCenteredText(canvas, detail.GradeDisplay!, regular, 30, secondary, 630, 900);
        }

        DrawCenteredText(canvas, $"在售 {detail.ProductCount} 款 · 粉丝 {detail.FollowerCount}", regular, 30, tertiary, 690, 900);

        using (var divider = new SKPaint { Color = new SKColor(0xE2, 0xE8, 0xF0), StrokeWidth = 2, IsAntialias = true })
        {
            canvas.DrawLine(120, 755, PosterWidth - 120, 755, divider);
        }

        DrawQr(canvas, qrPng, (PosterWidth - 340) / 2, 805, 340);
        DrawCenteredText(canvas, "扫码进入店铺", regular, 34, primary, 1215, 900);
        DrawCenteredText(canvas, platformName, bold, 30, secondary, 1295, 900);
        DrawCenteredText(canvas, "更多认证商家与真实货源", regular, 26, tertiary, 1345, 900);

        return EncodePng(surface);
    }

    /// <summary>
    /// 合成轴承海报：平台主图 + 型号 + 品牌 + 类型与尺寸 + 认证商家数 + 二维码。
    /// 认证商家数为 0 时整句省略，不出现负面文案。
    /// </summary>
    private static async Task<byte[]?> RenderBearingPosterAsync(
        Guid id,
        byte[] qrPng,
        string platformName,
        IMediator mediator,
        IObjectStorageService storage,
        CancellationToken ct)
    {
        var detail = await mediator.Send(new GetBearingQuery { Id = id }, ct);
        if (detail is null) return null;

        var merchants = await mediator.Send(new GetMerchantsByBearingQuery
        {
            BearingId = id,
            OnlyOnSale = true,
            Page = 1,
            PageSize = 100
        }, ct);
        var verified = merchants?.Items.Count(m => m.IsVerified) ?? 0;

        using var surface = SKSurface.Create(new SKImageInfo(PosterWidth, PosterHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        const int imageHeight = 760;
        using (var placeholder = new SKPaint { Color = new SKColor(0xF1, 0xF5, 0xF9), IsAntialias = true })
        {
            canvas.DrawRect(0, 0, PosterWidth, imageHeight, placeholder);
        }

        var imageBitmap = await TryLoadBitmapAsync(storage, detail.Image3DUrl ?? detail.Image2DUrl, ct);
        if (imageBitmap is not null)
        {
            DrawCoverImage(canvas, imageBitmap, 0, 0, PosterWidth, imageHeight);
        }

        var bold = ResolveTypeface(true);
        var regular = ResolveTypeface(false);
        var primary = new SKColor(0x0F, 0x17, 0x2A);
        var secondary = new SKColor(0x47, 0x55, 0x69);
        var tertiary = new SKColor(0x64, 0x74, 0x8B);
        var accent = new SKColor(0x25, 0x63, 0xEB);

        var title = string.IsNullOrWhiteSpace(detail.BrandName)
            ? detail.PartNumber
            : $"{detail.PartNumber} · {detail.BrandName}";
        DrawText(canvas, title, bold, 66, primary, 80, 870, 660);
        DrawText(canvas, $"{detail.BearingType}轴承", regular, 36, secondary, 80, 950, 660);
        DrawText(canvas, $"内径 {detail.InnerDiameter}  外径 {detail.OuterDiameter}  宽度 {detail.Width} mm",
            regular, 30, tertiary, 80, 1010, 660);

        if (verified > 0)
        {
            DrawText(canvas, $"{verified} 家认证商家真实有货", bold, 34, accent, 80, 1075, 660);
        }

        DrawQr(canvas, qrPng, PosterWidth - 340, 1090, 260);
        DrawText(canvas, "扫码查看在售商家", regular, 28, secondary, 80, 1200, 600);
        DrawText(canvas, platformName, bold, 30, primary, 80, 1290, 600);
        DrawText(canvas, "认证商家 · 真实货源", regular, 26, tertiary, 80, 1340, 600);

        return EncodePng(surface);
    }

    /// <summary>
    /// 选择中文字体：优先容器内的 Noto Sans CJK SC，其次开发机雅黑/苹方；都取不到用系统默认。
    /// </summary>
    private static SKTypeface ResolveTypeface(bool bold)
    {
        var style = bold ? SKFontStyle.Bold : SKFontStyle.Normal;
        foreach (var family in new[] { "Noto Sans CJK SC", "Noto Sans SC", "Microsoft YaHei", "PingFang SC", "WenQuanYi Zen Hei" })
        {
            var typeface = SKFontManager.Default.MatchFamily(family, style);
            if (typeface is not null)
            {
                return typeface;
            }
        }

        return SKTypeface.Default;
    }

    /// <summary>从对象存储读取图片并解码；缺失或异常返回 null（海报降级为纯文字/占位）。</summary>
    private static async Task<SKBitmap?> TryLoadBitmapAsync(IObjectStorageService storage, string? key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (key.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // 外部绝对地址不落对象存储，海报跳过配图
            return null;
        }

        var bytes = await storage.DownloadAsync(key.TrimStart('/'), ct);
        return bytes is null ? null : SKBitmap.Decode(bytes);
    }

    /// <summary>绘制商家 Logo（圆形裁切）；无 Logo 时画首字色块。</summary>
    private static void DrawLogo(SKCanvas canvas, SKBitmap? bitmap, string name, int size, int top)
    {
        var left = (PosterWidth - size) / 2;
        var rect = new SKRect(left, top, left + size, top + size);
        canvas.Save();
        using (var clip = new SKPath())
        {
            clip.AddCircle(rect.MidX, rect.MidY, size / 2f);
            canvas.ClipPath(clip, SKClipOperation.Intersect, true);
            if (bitmap is not null)
            {
                DrawCoverImage(canvas, bitmap, left, top, size, size);
            }
            else
            {
                using var fill = new SKPaint { Color = new SKColor(0x25, 0x63, 0xEB), IsAntialias = true };
                canvas.DrawRect(rect, fill);
                var initial = string.IsNullOrWhiteSpace(name) ? "店" : name.Substring(0, 1);
                DrawCenteredText(canvas, initial, ResolveTypeface(true), 96, SKColors.White, top + size / 2 + 34, size);
            }
        }
        canvas.Restore();

        using var border = new SKPaint
        {
            Color = new SKColor(0xE2, 0xE8, 0xF0),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawCircle(rect.MidX, rect.MidY, size / 2f, border);
    }

    /// <summary>绘制认证/类型徽标（圆角底 + 文字）。</summary>
    private static void DrawBadge(SKCanvas canvas, string text, SKTypeface typeface, float centerY)
    {
        using var font = new SKFont(typeface, 28);
        using var textPaint = new SKPaint { Color = new SKColor(0x1D, 0x4E, 0xD8), IsAntialias = true };
        var width = font.MeasureText(text);
        const float paddingX = 22;
        var badgeWidth = width + paddingX * 2;
        var left = (PosterWidth - badgeWidth) / 2;
        var rect = new SKRect(left, centerY - 26, left + badgeWidth, centerY + 26);

        using var bg = new SKPaint { Color = new SKColor(0xDB, 0xEA, 0xFE), IsAntialias = true };
        canvas.DrawRoundRect(rect, 26, 26, bg);
        canvas.DrawText(text, PosterWidth / 2f, centerY + 10, SKTextAlign.Center, font, textPaint);
    }

    /// <summary>绘制二维码（按目标尺寸缩放）。</summary>
    private static void DrawQr(SKCanvas canvas, byte[] qrPng, int left, int top, int size)
    {
        using var bitmap = SKBitmap.Decode(qrPng);
        if (bitmap is null) return;
        canvas.DrawBitmap(bitmap, new SKRect(left, top, left + size, top + size));
    }

    /// <summary>按目标区域做居中裁切绘制（cover），避免拉伸变形。</summary>
    private static void DrawCoverImage(SKCanvas canvas, SKBitmap bitmap, float left, float top, float width, float height)
    {
        var scale = Math.Max(width / bitmap.Width, height / bitmap.Height);
        var drawWidth = bitmap.Width * scale;
        var drawHeight = bitmap.Height * scale;
        var dest = new SKRect(
            left - (drawWidth - width) / 2,
            top - (drawHeight - height) / 2,
            left - (drawWidth - width) / 2 + drawWidth,
            top - (drawHeight - height) / 2 + drawHeight);
        canvas.Save();
        canvas.ClipRect(new SKRect(left, top, left + width, top + height));
        canvas.DrawBitmap(bitmap, dest);
        canvas.Restore();
    }

    /// <summary>左对齐绘制单行文字，超出宽度按省略号截断。</summary>
    private static void DrawText(SKCanvas canvas, string text, SKTypeface typeface, float size, SKColor color,
        float x, float y, float maxWidth)
    {
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(Truncate(font, text, maxWidth), x, y, SKTextAlign.Left, font, paint);
    }

    /// <summary>居中绘制单行文字，超出宽度按省略号截断。</summary>
    private static void DrawCenteredText(SKCanvas canvas, string text, SKTypeface typeface, float size, SKColor color,
        float y, float maxWidth)
    {
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(Truncate(font, text, maxWidth), PosterWidth / 2f, y, SKTextAlign.Center, font, paint);
    }

    /// <summary>按最大宽度截断文字，超长时追加省略号。</summary>
    private static string Truncate(SKFont font, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (font.MeasureText(text) <= maxWidth) return text;
        const string ellipsis = "…";
        for (var length = text.Length - 1; length > 0; length--)
        {
            var candidate = text.Substring(0, length) + ellipsis;
            if (font.MeasureText(candidate) <= maxWidth)
            {
                return candidate;
            }
        }
        return ellipsis;
    }

    /// <summary>把画布编码为 PNG 字节。</summary>
    private static byte[] EncodePng(SKSurface surface)
    {
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 92);
        return data.ToArray();
    }
}
