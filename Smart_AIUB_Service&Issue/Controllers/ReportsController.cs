using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Controllers;

/// <summary>Admin date-range reports.</summary>
public class ReportsController : AdminControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    /// <summary>Default range: the last 30 days, ending today (Bangladesh date).</summary>
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var end = to ?? DhakaTime.TodayInDhaka();
        var start = from ?? end.AddDays(-29);

        if (start > end)
        {
            // Friendly fix instead of an error page: swap the two dates.
            (start, end) = (end, start);
            TempData["Error"] = "The start date was after the end date, so the two dates were swapped.";
        }

        return View(await _reports.GetAsync(start, end));
    }
}
