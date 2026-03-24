namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents one column discovered from the source database schema.
/// </summary>
public sealed class TableColumnMetadata
{
    /// <summary>
    /// Gets or sets the source table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nullability flag from the schema.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the key classification reported by MySQL.
    /// </summary>
    public string ColumnKey { get; set; } = string.Empty;
}
