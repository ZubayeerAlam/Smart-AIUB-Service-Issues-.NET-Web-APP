namespace Smart_AIUB_Service_Issue.Authorization;

/// <summary>
/// Role names in one place, so a typo like "admin" vs "Admin" becomes a compile error
/// instead of a silent security bug.
/// </summary>
public static class Roles
{
    public const string Student = "Student";
    public const string Faculty = "Faculty";
    public const string SupportStaff = "SupportStaff";
    public const string Admin = "Admin";

    public static readonly string[] All = [Student, Faculty, SupportStaff, Admin];

    /// <summary>The only roles a user may pick on the public Register page.</summary>
    public static readonly string[] SelfRegister = [Student, Faculty];
}
