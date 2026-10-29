using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services;

public class SupportTeamService : ISupportTeamService
{
    private readonly ApplicationDbContext _db;

    public SupportTeamService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<SupportTeamListItem>> GetPageAsync(string? search, string? status, int page)
    {
        var query = _db.SupportTeams.AsNoTracking().WhereStatus(status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Name.Contains(term));
        }

        return query
            .OrderBy(t => t.Name)
            .Select(t => new SupportTeamListItem(t.Id, t.Name, t.Description, t.Members.Count, t.IsActive))
            .ToPagedResultAsync(page);
    }

    public Task<SupportTeamFormViewModel?> GetFormAsync(int id) =>
        _db.SupportTeams.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new SupportTeamFormViewModel { Id = t.Id, Name = t.Name, Description = t.Description })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(SupportTeamFormViewModel model)
    {
        var name = model.Name.Trim();

        if (await _db.SupportTeams.AnyAsync(t => t.Id != model.Id && t.Name == name))
        {
            return ServiceResult.Fail($"A team named '{name}' already exists.", nameof(model.Name));
        }

        SupportTeam? team;
        if (model.Id == 0)
        {
            team = new SupportTeam();
            _db.SupportTeams.Add(team);
        }
        else
        {
            team = await _db.SupportTeams.FindAsync(model.Id);
            if (team is null)
            {
                return ServiceResult.Fail("Team not found.");
            }
        }

        team.Name = name;
        team.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive) =>
        await _db.SetActiveAsync<SupportTeam>(id, isActive)
            ? ServiceResult.Success()
            : ServiceResult.Fail("Team not found.");

    public Task<List<SelectListItem>> GetOptionsAsync(int? includeId = null) =>
        _db.SupportTeams.AsNoTracking()
            .Where(t => t.IsActive || t.Id == includeId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.IsActive ? t.Name : t.Name + " - inactive", t.Id.ToString()))
            .ToListAsync();
}
