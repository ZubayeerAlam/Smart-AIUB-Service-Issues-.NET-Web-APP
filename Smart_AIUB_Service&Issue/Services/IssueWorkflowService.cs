using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Services;

public class IssueWorkflowService : IIssueWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<IssueWorkflowService> _logger;

    public IssueWorkflowService(ApplicationDbContext db, ILogger<IssueWorkflowService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult> CancelAsync(int issueId, string? reason, IssueActor actor)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == issueId);
        if (issue is null)
        {
            return ServiceResult.Fail("Issue not found.");
        }
        if (!IssuePermissions.CanCancel(issue, actor))
        {
            return ServiceResult.Fail("Only a submitted issue that hasn't been reviewed yet can be cancelled.");
        }

        var note = string.IsNullOrWhiteSpace(reason) ? "Cancelled by reporter" : reason.Trim();
        return await ChangeStatusAsync(issue, IssueStatus.Cancelled, actor, note);
    }

    /// <summary>Sets the new status, writes the history row and saves both together.</summary>
    private async Task<ServiceResult> ChangeStatusAsync(Issue issue, IssueStatus to, IssueActor actor, string? note)
    {
        var now = DateTime.UtcNow;
        var from = issue.Status;

        issue.Status = to;
        issue.UpdatedAt = now;
        _db.IssueStatusHistory.Add(new IssueStatusHistory
        {
            IssueId = issue.Id,
            FromStatus = from,
            ToStatus = to,
            ChangedById = actor.UserId,
            ChangedAt = now,
            Note = note
        });

        try
        {
            // One SaveChanges = one database transaction: status and history are saved together.
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This issue was changed by someone else just now. Please reload and try again.");
        }

        _logger.LogInformation("Issue {IssueId}: {From} -> {To} by {UserId}", issue.Id, from, to, actor.UserId);
        return ServiceResult.Success();
    }
}
