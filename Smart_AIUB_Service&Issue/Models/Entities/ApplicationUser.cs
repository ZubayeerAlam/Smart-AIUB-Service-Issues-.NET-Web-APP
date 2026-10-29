using Microsoft.AspNetCore.Identity;

namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>
/// Login account plus profile. Identity already provides Id, Email, UserName,
/// PasswordHash, lockout fields, etc. We only add campus-specific fields.
/// </summary>
public class ApplicationUser : IdentityUser, IActivatable
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Student ID or employee ID. Optional (e.g. the seeded admin has none).</summary>
    public string? UniversityId { get; set; }

    /// <summary>Academic department (Students and Faculty).</summary>
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    /// <summary>Support team (Support Staff only).</summary>
    public int? SupportTeamId { get; set; }
    public SupportTeam? SupportTeam { get; set; }

    /// <summary>Deactivated users cannot log in, but their history is kept.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
