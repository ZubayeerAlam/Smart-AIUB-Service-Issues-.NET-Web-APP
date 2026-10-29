using Smart_AIUB_Service_Issue.Data;
using Smart_AIUB_Service_Issue.Models.Entities;

namespace Smart_AIUB_Service_Issue.Common;

public static class StatusFilter
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

public static class ActivatableExtensions
{
    /// <summary>Filters a list by the "Status" dropdown: active, inactive, or everything.</summary>
    public static IQueryable<T> WhereStatus<T>(this IQueryable<T> query, string? status)
        where T : class, IActivatable =>
        status switch
        {
            StatusFilter.Active => query.Where(x => x.IsActive),
            StatusFilter.Inactive => query.Where(x => !x.IsActive),
            _ => query
        };

    /// <summary>Activates or deactivates one row. Returns false if the id does not exist.</summary>
    public static async Task<bool> SetActiveAsync<T>(this ApplicationDbContext db, int id, bool isActive)
        where T : class, IActivatable
    {
        var entity = await db.Set<T>().FindAsync(id);
        if (entity is null)
        {
            return false;
        }

        entity.IsActive = isActive;
        await db.SaveChangesAsync();
        return true;
    }
}
