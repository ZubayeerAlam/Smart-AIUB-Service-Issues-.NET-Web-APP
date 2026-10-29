using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Smart_AIUB_Service_Issue.Authorization;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Models.Enums;
using Smart_AIUB_Service_Issue.Services.Interfaces;

namespace Smart_AIUB_Service_Issue.Services;

public class IssueWorkflowService : IIssueWorkflowService
{
    private const int MaxNoteLength = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IssueOptions _options;
    private readonly ILogger<IssueWorkflowService> _logger;

    public IssueWorkflowService(ApplicationDbContext db, IOptions<IssueOptions> options, ILogger<IssueWorkflowService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    // ---------- Admin review ----------

    public async Task<ServiceResult> AssignAsync(
        int issueId, IssuePriority? priority, int? teamId, string? staffId, string? note, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanAssign(issue, actor)) return NotAllowed("assigned", issue);

        if (priority is null)
        {
            return ServiceResult.Fail("Please choose a priority.", "Priority");
        }

        var team = await _db.SupportTeams.FirstOrDefaultAsync(t => t.Id == teamId && t.IsActive);
        if (team is null)
        {
            return ServiceResult.Fail("Please choose an active support team.", "TeamId");
        }

        ApplicationUser? staff = null;
        if (!string.IsNullOrEmpty(staffId))
        {
            staff = await _db.Users.FirstOrDefaultAsync(u => u.Id == staffId && u.IsActive && u.SupportTeamId == team.Id);
            var isStaffRole = staff is not null && await (
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                where ur.UserId == staffId && r.Name == Roles.SupportStaff
                select ur).AnyAsync();
            if (!isStaffRole)
            {
                return ServiceResult.Fail($"The technician must be active support staff in {team.Name}.", "StaffId");
            }
        }

        if (note?.Length > MaxNoteLength) return TooLong();

        var isReassign = issue.Status != IssueStatus.Submitted;
        issue.Priority = priority;
        issue.AssignedTeamId = team.Id;
        issue.AssignedStaffId = staff?.Id;

        var summary = $"{(isReassign ? "Reassigned" : "Assigned")} to {team.Name}" +
                      (staff is null ? "" : $" ({staff.FullName})") +
                      $" · Priority: {priority}";
        return await ChangeStatusAsync(issue, IssueStatus.Assigned, actor, Combine(summary, note));
    }

    public async Task<ServiceResult> RejectAsync(int issueId, string? reason, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanReject(issue, actor)) return NotAllowed("rejected", issue);

        var error = RequireText(reason, "Please give a reason for rejecting this issue.");
        if (error is not null) return error;

        return await ChangeStatusAsync(issue, IssueStatus.Rejected, actor, reason!.Trim());
    }

    // ---------- Support staff ----------

    public async Task<ServiceResult> TakeAsync(int issueId, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanTake(issue, actor))
        {
            return ServiceResult.Fail("This issue is not in your team's unassigned queue (someone may have taken it already).");
        }

        issue.AssignedStaffId = actor.UserId;
        var name = await UserNameAsync(actor.UserId);
        // Status stays Assigned; the history row records who took it.
        return await ChangeStatusAsync(issue, IssueStatus.Assigned, actor, $"Taken by {name}");
    }

    public async Task<ServiceResult> StartAsync(int issueId, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanStart(issue, actor)) return NotAllowed("started", issue);

        // A team member who starts an unassigned issue becomes its technician.
        if (issue.AssignedStaffId is null && actor.IsStaff)
        {
            issue.AssignedStaffId = actor.UserId;
        }
        return await ChangeStatusAsync(issue, IssueStatus.InProgress, actor, "Work started");
    }

    public async Task<ServiceResult> HoldAsync(int issueId, string? reason, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanHold(issue, actor)) return NotAllowed("put on hold", issue);

        var error = RequireText(reason, "Please say why the work is on hold (e.g. waiting for parts).");
        if (error is not null) return error;

        return await ChangeStatusAsync(issue, IssueStatus.OnHold, actor, reason!.Trim());
    }

    public async Task<ServiceResult> ResumeAsync(int issueId, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanResume(issue, actor)) return NotAllowed("resumed", issue);

        return await ChangeStatusAsync(issue, IssueStatus.InProgress, actor, "Work resumed");
    }

    public async Task<ServiceResult> ResolveAsync(int issueId, string? resolutionNote, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanResolve(issue, actor)) return NotAllowed("resolved", issue);

        var error = RequireText(resolutionNote, "Please describe what was done to fix the problem.", minLength: 10);
        if (error is not null) return error;

        issue.ResolutionNote = resolutionNote!.Trim();
        issue.ResolvedAt = DateTime.UtcNow;
        return await ChangeStatusAsync(issue, IssueStatus.Resolved, actor, issue.ResolutionNote);
    }

    // ---------- Reporter ----------

    public async Task<ServiceResult> CancelAsync(int issueId, string? reason, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanCancel(issue, actor))
        {
            return ServiceResult.Fail("Only a submitted issue that hasn't been reviewed yet can be cancelled.");
        }
        if (reason?.Length > 500) return TooLong();

        var note = string.IsNullOrWhiteSpace(reason) ? "Cancelled by reporter" : reason.Trim();
        return await ChangeStatusAsync(issue, IssueStatus.Cancelled, actor, note);
    }

    public async Task<ServiceResult> CloseAsync(int issueId, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanClose(issue, actor)) return NotAllowed("closed", issue);

        issue.ClosedAt = DateTime.UtcNow;
        var note = IssuePermissions.IsReporter(issue, actor) ? "Fix confirmed by reporter" : "Closed by admin";
        return await ChangeStatusAsync(issue, IssueStatus.Closed, actor, note);
    }

    public async Task<ServiceResult> ReopenAsync(int issueId, string? reason, IssueActor actor)
    {
        var issue = await FindAsync(issueId);
        if (issue is null) return NotFound();
        if (!IssuePermissions.CanReopen(issue, actor, DateTime.UtcNow, _options.ReopenWindowDays))
        {
            return ServiceResult.Fail(
                $"Only the reporter can reopen a resolved issue, within {_options.ReopenWindowDays} days. Please report a new issue instead.");
        }

        var error = RequireText(reason, "Please say what is still wrong.");
        if (error is not null) return error;

        // Back to the same team and technician.
        issue.ReopenCount++;
        issue.ResolvedAt = null;
        issue.ResolutionNote = null; // the old note stays in the timeline
        return await ChangeStatusAsync(issue, IssueStatus.Assigned, actor, "Reopened: " + reason!.Trim());
    }

    // ---------- Helpers ----------

    private Task<Issue?> FindAsync(int id) => _db.Issues.FirstOrDefaultAsync(i => i.Id == id);

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
            Note = note is { Length: > 1000 } ? note[..997] + "..." : note
        });

        try
        {
            // One SaveChanges = one database transaction: status and history are saved together.
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion changed: another person updated this issue at the same moment.
            return ServiceResult.Fail("This issue was changed by someone else just now. Please reload and try again.");
        }

        _logger.LogInformation("Issue {IssueId}: {From} -> {To} by {UserId}", issue.Id, from, to, actor.UserId);
        return ServiceResult.Success();
    }

    private async Task<string> UserNameAsync(string userId) =>
        await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "staff";

    private static ServiceResult? RequireText(string? text, string message, int minLength = 3)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < minLength)
        {
            return ServiceResult.Fail(message, "Note");
        }
        return text.Length > MaxNoteLength ? TooLong() : null;
    }

    private static string Combine(string summary, string? note) =>
        string.IsNullOrWhiteSpace(note) ? summary : $"{summary}\n{note.Trim()}";

    private static ServiceResult NotFound() => ServiceResult.Fail("Issue not found.");

    private static ServiceResult TooLong() => ServiceResult.Fail($"The text is too long (max {MaxNoteLength} characters).", "Note");

    private static ServiceResult NotAllowed(string action, Issue issue) =>
        ServiceResult.Fail($"This issue cannot be {action} by you while it is {issue.Status.DisplayName()}.");
}
