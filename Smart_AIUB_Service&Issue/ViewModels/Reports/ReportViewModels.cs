using Smart_AIUB_Service_Issue.ViewModels.Dashboard;

namespace Smart_AIUB_Service_Issue.ViewModels.Reports;

/// <summary>Average resolution time for one group (e.g. one priority).</summary>
public record ResolutionRow(string Label, int Count, double AverageHours);

/// <summary>Maintenance count and cost for one group (type, asset type or asset).</summary>
public record CostRow(string Label, int Count, decimal Cost);

/// <summary>Admin date-range report. All dates are Bangladesh dates and both ends are included.</summary>
public class ReportViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int Days => To.DayNumber - From.DayNumber + 1;

    // Issues reported in the range
    public int IssuesReported { get; set; }
    public BarList ByStatus { get; set; } = new("", []);
    public BarList ByCategory { get; set; } = new("", []);
    public BarList ByBuilding { get; set; } = new("", []);
    public BarList ByTeam { get; set; } = new("", []);

    // Issues resolved in the range (time from reported to resolved)
    public int ResolvedCount { get; set; }
    public double? AverageHours { get; set; }
    public double? MedianHours { get; set; }
    public List<ResolutionRow> ResolutionByPriority { get; set; } = [];
    public List<ResolutionRow> ResolutionByCategory { get; set; } = [];

    // Maintenance performed in the range
    public int MaintenanceCount { get; set; }
    public decimal MaintenanceCost { get; set; }
    public List<CostRow> CostByMaintenanceType { get; set; } = [];
    public List<CostRow> CostByAssetType { get; set; } = [];
    public List<CostRow> TopAssetsByCost { get; set; } = [];

    /// <summary>36.5 → "1 d 12 h", 5.25 → "5 h 15 m", 0.4 → "24 m".</summary>
    public static string Duration(double? hours)
    {
        if (hours is null) return "—";
        var span = TimeSpan.FromHours(hours.Value);
        if (span.TotalDays >= 1) return $"{(int)span.TotalDays} d {span.Hours} h";
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours} h {span.Minutes} m";
        return $"{Math.Max(1, (int)Math.Round(span.TotalMinutes))} m";
    }
}
