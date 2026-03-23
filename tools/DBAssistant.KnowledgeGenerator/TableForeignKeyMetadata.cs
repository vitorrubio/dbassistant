namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents one foreign-key relationship discovered from the source database schema.
/// </summary>
public sealed class TableForeignKeyMetadata
{
    /// <summary>
    /// Gets or sets the foreign-key constraint name.
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the referenced table name.
    /// </summary>
    public string ReferencedTableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the referenced column name.
    /// </summary>
    public string ReferencedColumnName { get; set; } = string.Empty;
}
