using Microsoft.EntityFrameworkCore;
using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;
using Smart_AIUB_Service_Issue.Services.Interfaces;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services;

public class IssueCategoryService : IIssueCategoryService
{
    private readonly ApplicationDbContext _db;

    public IssueCategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<IssueCategoryListItem>> GetPageAsync(string? search, string? status, int page)
    {
        var query = _db.IssueCategories.AsNoTracking().WhereStatus(status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term));
        }

        return query
            .OrderBy(c => c.Name)
            .Select(c => new IssueCategoryListItem(
                c.Id, c.Name, c.Description,
                c.DefaultSupportTeam != null ? c.DefaultSupportTeam.Name : null,
                c.IsActive))
            .ToPagedResultAsync(page);
    }

    public Task<IssueCategoryFormViewModel?> GetFormAsync(int id) =>
        _db.IssueCategories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new IssueCategoryFormViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DefaultSupportTeamId = c.DefaultSupportTeamId
            })
            .FirstOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(IssueCategoryFormViewModel model)
    {
        var name = model.Name.Trim();

        if (await _db.IssueCategories.AnyAsync(c => c.Id != model.Id && c.Name == name))
        {
            return ServiceResult.Fail($"A category named '{name}' already exists.", nameof(model.Name));
        }

        IssueCategory? category;
        if (model.Id == 0)
        {
            category = new IssueCategory();
            _db.IssueCategories.Add(category);
        }
        else
        {
            category = await _db.IssueCategories.FindAsync(model.Id);
            if (category is null)
            {
                return ServiceResult.Fail("Category not found.");
            }
        }

        // A new default team must be active; keeping the current (now inactive) one is allowed.
        if (model.DefaultSupportTeamId is not null && model.DefaultSupportTeamId != category.DefaultSupportTeamId)
        {
            var teamIsActive = await _db.SupportTeams.AnyAsync(t => t.Id == model.DefaultSupportTeamId && t.IsActive);
            if (!teamIsActive)
            {
                return ServiceResult.Fail("Please select an active support team.", nameof(model.DefaultSupportTeamId));
            }
        }

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        category.DefaultSupportTeamId = model.DefaultSupportTeamId;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive) =>
        await _db.SetActiveAsync<IssueCategory>(id, isActive)
            ? ServiceResult.Success()
            : ServiceResult.Fail("Category not found.");
}
