using Microsoft.AspNetCore.Identity;

namespace Smart_AIUB_Service_Issue.Authorization;

/// <summary>
/// Password, lockout and user rules in one place, used by Program.cs and by the
/// tests, so both always follow the same rules.
/// </summary>
public static class IdentityConfig
{
    public static void Configure(IdentityOptions options)
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // 5 wrong passwords => account locked for 15 minutes.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false; // no email sending in Release 1
    }
}
