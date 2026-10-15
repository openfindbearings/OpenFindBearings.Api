namespace OpenFindBearings.Domain.Services;

/// <summary>
/// 业务时钟（v1.36.1 新增）：全项目"今天"语义的唯一出口。
/// 职责：数据库存储与写入保持纯 UTC（DateTime.UtcNow 规范不变），
/// 但"日任务/额度/连签"这类按天归边的业务判定必须按中国用户零点切日——
/// 直接用 UTC 日界会把北京时间 0-8 点的动作归入前一天（bizId 撞昨日键导致漏发分）。
/// 上层代码一律通过本类取业务日，不再各自决定用哪个"今天"。
/// </summary>
public static class BusinessClock
{
    /// <summary>
    /// 业务时区偏移（默认北京时间 UTC+8，WordPress 式配置：存 DB 由管理员定义，非读服务器时区——
    /// 容器/宿主机时区属部署细节，且本项目 Npgsql 连接串钉死 Timezone=UTC，从 DB 会话读时区必得 UTC）
    /// </summary>
    private static TimeSpan _businessOffset = TimeSpan.FromHours(8);

    /// <summary>
    /// 配置业务日界偏移（启动时由 Program.cs 读 SystemConfig Business.TimeZoneOffsetHours 调用）。
    /// 合法范围 -12~+14（全球时区偏移域）；越界值忽略保留默认，防误配炸掉全站日界
    /// </summary>
    /// <param name="offsetHours">偏移小时数</param>
    public static void Configure(int offsetHours)
    {
        if (offsetHours is >= -12 and <= 14)
        {
            _businessOffset = TimeSpan.FromHours(offsetHours);
        }
    }

    /// <summary>业务"现在"（业务时区墙上时钟）</summary>
    public static DateTime Now => DateTime.UtcNow.Add(_businessOffset);

    /// <summary>业务日零点（业务日历的今天 00:00，仅用于日期比较与键生成，不直接进 DB 查询）</summary>
    public static DateTime Today => Now.Date;

    /// <summary>业务日零点对应的 UTC 时刻——DB 范围查询专用（CreatedAt &gt;= TodayUtc 即"业务今天"）</summary>
    public static DateTime TodayUtc => DateTime.SpecifyKind(Today - _businessOffset, DateTimeKind.Utc);

    /// <summary>业务日"昨天"零点对应的 UTC 时刻（连签连续性判断用）</summary>
    public static DateTime YesterdayUtc => TodayUtc - TimeSpan.FromDays(1);

    /// <summary>当前业务偏移——供"任意业务日历时刻 → UTC"换算（如周/月统计起点减偏移）</summary>
    public static TimeSpan Offset => _businessOffset;

    /// <summary>
    /// 业务周起点（本周一 00:00）对应的 UTC 时刻——周统计范围查询专用。
    /// 改动说明（v2.13.2 周起点 bug 修复）：原多处自算 <c>AddDays(-(int)DayOfWeek + (int)Monday)</c>，
    /// 周日（DayOfWeek.Sunday=0）会得到"明天的周一"（未来日）致周统计归零；改用 (dow+6)%7 收敛唯一实现
    /// </summary>
    public static DateTime BusinessWeekStartUtc
    {
        get
        {
            var daysSinceMonday = ((int)Today.DayOfWeek + 6) % 7;
            return DateTime.SpecifyKind(Today.AddDays(-daysSinceMonday) - _businessOffset, DateTimeKind.Utc);
        }
    }

    /// <summary>
    /// 业务月起点（本月 1 日 00:00）对应的 UTC 时刻——月统计/月榜范围查询专用。
    /// 改动说明（v2.13.2 收敛）：原四处各自 new DateTime(y,m,1) 再减偏移，统一到此
    /// </summary>
    public static DateTime BusinessMonthStartUtc =>
        DateTime.SpecifyKind(new DateTime(Today.Year, Today.Month, 1) - _businessOffset, DateTimeKind.Utc);

    /// <summary>bizId 日期键（yyyyMMdd，业务日历），如 checkin:{userId}:20260925</summary>
    public static string DateKey => Now.ToString("yyyyMMdd");
}
