namespace Smart_AIUB_Service_Issue.Models.Enums;

public enum MaintenanceType
{
    /// <summary>Fixing something that is broken (usually linked to an issue).</summary>
    Corrective,

    /// <summary>Planned servicing to prevent failures.</summary>
    Preventive,

    /// <summary>A check-up with no repair needed.</summary>
    Inspection
}
