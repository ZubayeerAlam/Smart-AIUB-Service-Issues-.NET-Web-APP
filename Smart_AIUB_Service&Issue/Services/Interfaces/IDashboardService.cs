using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.ViewModels.Dashboard;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

/// <summary>Read-only numbers and short lists for the three role dashboards.</summary>
public interface IDashboardService
{
    Task<ReporterDashboardViewModel> GetReporterAsync(string userId);
    Task<StaffDashboardViewModel> GetStaffAsync(IssueActor actor);
    Task<AdminDashboardViewModel> GetAdminAsync();
}
