using Smart_AIUB_Service_Issue.ViewModels.Issues;

namespace Smart_AIUB_Service_Issue.ViewModels.Dashboard;

/// <summary>One number tile, e.g. "Waiting for review: 4". Url makes the tile clickable.</summary>
public record StatTile(string Label, int Value, string Hint, string? Url = null, string Tone = "blue");

/// <summary>One bar in a simple horizontal bar list (e.g. open issues per building).</summary>
public record BarItem(string Label, int Count, string? Url = null);

public record BarList(string Title, IReadOnlyList<BarItem> Items)
{
    public int Max => Items.Count == 0 ? 0 : Items.Max(i => i.Count);
}

/// <summary>A short issue list on a dashboard, with the text to show when it is empty.</summary>
public record MiniListModel(IReadOnlyList<IssueListItem> Items, string EmptyText);

/// <summary>Students and faculty.</summary>
public class ReporterDashboardViewModel
{
    public string FullName { get; set; } = "";
    public List<StatTile> Tiles { get; set; } = [];

    /// <summary>Resolved issues waiting for the reporter to confirm or reopen.</summary>
    public List<IssueListItem> NeedsConfirmation { get; set; } = [];
    public List<IssueListItem> Recent { get; set; } = [];
}

/// <summary>Support staff.</summary>
public class StaffDashboardViewModel
{
    public string FullName { get; set; } = "";
    public string? TeamName { get; set; }
    public List<StatTile> Tiles { get; set; } = [];
    public List<IssueListItem> MyActive { get; set; } = [];
    public List<IssueListItem> Unassigned { get; set; } = [];
}

/// <summary>Admins: the whole campus at a glance.</summary>
public class AdminDashboardViewModel
{
    public List<StatTile> Tiles { get; set; } = [];
    public BarList ByPriority { get; set; } = new("", []);
    public BarList ByCategory { get; set; } = new("", []);
    public BarList ByBuilding { get; set; } = new("", []);
    public BarList ByTeam { get; set; } = new("", []);
    public List<IssueListItem> OldestWaiting { get; set; } = [];
    public List<IssueListItem> RecentlyResolved { get; set; } = [];
}
