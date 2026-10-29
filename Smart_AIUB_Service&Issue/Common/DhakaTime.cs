namespace Smart_AIUB_Service_Issue.Common;

/// <summary>
/// The database stores every time in UTC. People on campus think in Bangladesh time
/// (UTC+6), so these helpers convert for display.
/// </summary>
public static class DhakaTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");

    public static DateTime ToDhaka(this DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>e.g. "29 Oct 2026, 3:05 PM"</summary>
    public static string ToDisplay(this DateTime utc) => utc.ToDhaka().ToString("d MMM yyyy, h:mm tt");

    /// <summary>e.g. "29 Oct 2026"</summary>
    public static string ToDisplayDate(this DateTime utc) => utc.ToDhaka().ToString("d MMM yyyy");

    /// <summary>Converts a Dhaka wall-clock time (e.g. typed in a form) to UTC for saving.</summary>
    public static DateTime FromDhakaToUtc(DateTime dhakaLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dhakaLocal, DateTimeKind.Unspecified), Zone);

    /// <summary>Today's date in Bangladesh (which can differ from the UTC date between 18:00 and 24:00 UTC).</summary>
    public static DateOnly TodayInDhaka() => DateOnly.FromDateTime(DateTime.UtcNow.ToDhaka());

    /// <summary>
    /// Turns an inclusive Bangladesh date range ("1 Oct to 31 Oct") into a UTC range for queries:
    ///   start = 1 Oct 00:00 Dhaka (= 30 Sep 18:00 UTC), end = 1 Nov 00:00 Dhaka (exclusive).
    /// Use it as  utc >= start &amp;&amp; utc &lt; end. Either side may be open (null).
    /// </summary>
    public static (DateTime? StartUtc, DateTime? EndUtcExclusive) ToUtcRange(DateOnly? from, DateOnly? to)
    {
        DateTime? start = from is null ? null : FromDhakaToUtc(from.Value.ToDateTime(TimeOnly.MinValue));
        DateTime? end = to is null ? null : FromDhakaToUtc(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        return (start, end);
    }

    /// <summary>Current Bangladesh time, rounded to the minute (for form defaults).</summary>
    public static DateTime NowInDhaka()
    {
        var now = DateTime.UtcNow.ToDhaka();
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
    }
}
