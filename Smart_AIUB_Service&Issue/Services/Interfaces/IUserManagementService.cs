using Smart_AIUB_Service_Issue.Common;
using Smart_AIUB_Service_Issue.ViewModels.Admin;

namespace Smart_AIUB_Service_Issue.Services.Interfaces;

public interface IUserManagementService
{
    Task<PagedResult<UserListItem>> GetPageAsync(string? search, string? status, string? role, int page);

    /// <summary>Creates a SupportStaff (needs an active team) or Admin account.</summary>
    Task<ServiceResult> CreateAsync(CreateUserViewModel model);

    Task<EditUserViewModel?> GetEditAsync(string id);

    /// <summary>
    /// Updates name/ID and, depending on the user's role, their department (Student/Faculty)
    /// or support team (SupportStaff).
    /// </summary>
    Task<ServiceResult> UpdateAsync(EditUserViewModel model);

    /// <summary>
    /// Activates or deactivates a user. Deactivating also changes their security stamp,
    /// so an existing login cookie stops working within ~3 minutes. Admins cannot deactivate themselves.
    /// </summary>
    Task<ServiceResult> SetActiveAsync(string id, bool isActive, string currentUserId);

    Task<ResetPasswordViewModel?> GetResetPasswordAsync(string id);

    /// <summary>Sets a new password and clears any lockout.</summary>
    Task<ServiceResult> ResetPasswordAsync(string id, string newPassword);
}
