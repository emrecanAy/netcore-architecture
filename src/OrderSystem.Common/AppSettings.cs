namespace OrderSystem.Common;

/// <summary>
/// Strongly-typed application configuration. Bound from configuration at startup.
/// Secrets must come from environment/KeyVault, never hard-coded.
/// </summary>
public class AppSettings
{
    public const string SectionName = "App";

    /// <summary>EF Core connection string (SQLite file path for local dev).</summary>
    public string ConnectionString { get; set; } = "Data Source=ordersystem.db";

    /// <summary>Default currency for monetary values created without an explicit currency.</summary>
    public string DefaultCurrency { get; set; } = "USD";
}
