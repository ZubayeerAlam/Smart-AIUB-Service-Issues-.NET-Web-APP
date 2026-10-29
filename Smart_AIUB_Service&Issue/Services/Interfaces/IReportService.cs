using Smart_AIUB_Service_Issue.ViewModels.Reports;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IReportService
{
    /// <summary>
    /// Issues reported, issues resolved (with resolution time) and maintenance cost between two
    /// Bangladesh dates, both included. <paramref name="from"/> must not be after <paramref name="to"/>.
    /// </summary>
    Task<ReportViewModel> GetAsync(DateOnly from, DateOnly to);
}
