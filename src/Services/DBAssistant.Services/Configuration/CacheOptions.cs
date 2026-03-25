namespace DBAssistant.Services.Configuration;

/// <summary>
/// Stores cache durations used by external integrations.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Gets or sets the number of minutes that generated SQL plans stay in memory.
    /// </summary>
    public int SqlPlanMinutes { get; set; } = 15;

}
