namespace Smart_AIUB_Service_Issue.Models.Entities;

/// <summary>
/// Entities that are deactivated instead of deleted. Lets one helper filter and
/// toggle all of them (see Common/ActivatableExtensions).
/// </summary>
public interface IActivatable
{
    bool IsActive { get; set; }
}
