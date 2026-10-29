namespace Smart_AIUB_Service_Issue.Authorization;

/// <summary>
/// Named authorization policies. Use them on controllers/actions, e.g.
/// [Authorize(Policy = Policies.AdminOnly)]. They are registered in Program.cs.
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOrAdmin = "StaffOrAdmin";
}
