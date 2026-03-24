namespace DBAssistant.Api.IntegrationTests.Acceptance;

/// <summary>
/// Represents one acceptance scenario mapped to a natural-language question and a deterministic SQL statement.
/// </summary>
public sealed class AcceptanceScenario
{
    /// <summary>
    /// Gets or sets the natural-language question sent to the controller.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the deterministic SQL used by the fake generation gateway.
    /// </summary>
    public string Sql { get; init; } = string.Empty;
}
