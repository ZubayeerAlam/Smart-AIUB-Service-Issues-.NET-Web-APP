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

    /// <summary>Current Bangladesh time, rounded to the minute (for form defaults).</summary>
    public static DateTime NowInDhaka()
    {
        var now = DateTime.UtcNow.ToDhaka();
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
    }
}
