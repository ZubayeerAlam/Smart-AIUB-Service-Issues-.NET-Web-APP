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
}
