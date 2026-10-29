using Smart_AIUB_Service_Issue.Common;

namespace Smart_AIUB_Service_Issue.ViewModels.Admin;

/// <summary>Search box + status dropdown values, used by the shared _ListFilter partial.</summary>
public interface IListFilter
{
    string? Search { get; }
    string? Status { get; }
}

/// <summary>A searchable, paged admin list (Departments, Buildings, ...).</summary>
public class ListPageViewModel<T> : IListFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public PagedResult<T> Results { get; set; } = null!;
}

/// <summary>Data for the shared Activate/Deactivate button (_ActiveToggle partial).</summary>
public record ActiveToggleModel(string Id, bool IsActive, string ItemName);
