using Smart_AIUB_Service_Issue.Common;

namespace Smart_AIUB_Service_Issue.Tests.Helpers;

/// <summary>
/// Bangladesh is UTC+6 with no daylight saving. The tricky moments are around 18:00 UTC,
/// which is already midnight of the NEXT day in Dhaka.
/// </summary>
public class DhakaTimeTests
{
    private static DateTime Utc(int y, int m, int d, int h, int min, int s = 0) => new(y, m, d, h, min, s, DateTimeKind.Utc);

    [Fact]
    public void Date_range_is_converted_to_a_utc_window_that_includes_both_whole_days()
    {
        var (start, end) = DhakaTime.ToUtcRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal(Utc(2026, 9, 30, 18, 0), start);   // 1 Oct 00:00 in Dhaka
        Assert.Equal(Utc(2026, 10, 31, 18, 0), end);    // 1 Nov 00:00 in Dhaka (exclusive)
    }

    [Fact]
    public void Single_day_range_covers_24_hours()
    {
        var (start, end) = DhakaTime.ToUtcRange(new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 29));

        Assert.Equal(TimeSpan.FromHours(24), end - start);
    }

    [Fact]
    public void Open_ended_ranges_leave_that_side_empty()
    {
        var (start, end) = DhakaTime.ToUtcRange(null, new DateOnly(2026, 10, 31));

        Assert.Null(start);
        Assert.NotNull(end);
        Assert.Equal((null, null), DhakaTime.ToUtcRange(null, null));
    }

    [Theory]
    [InlineData(17, 59, 31)] // 31 Oct 23:59 in Dhaka
    [InlineData(18, 0, 1)]   //  1 Nov 00:00 in Dhaka
    public void Utc_evening_can_already_be_the_next_day_in_Dhaka(int hour, int minute, int expectedDhakaDay)
    {
        var dhaka = Utc(2026, 10, 31, hour, minute).ToDhaka();

        Assert.Equal(expectedDhakaDay, dhaka.Day);
    }

    [Fact]
    public void Form_time_in_Dhaka_is_saved_as_utc()
    {
        var utc = DhakaTime.FromDhakaToUtc(new DateTime(2026, 11, 1, 0, 30, 0));

        Assert.Equal(Utc(2026, 10, 31, 18, 30), utc);
    }
}
