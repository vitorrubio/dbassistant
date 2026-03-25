namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents the aggregated metadata of one source table used to generate RAG documents.
/// </summary>
public sealed class TableMetadata
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table type reported by MySQL, such as BASE TABLE or VIEW.
    /// </summary>
    public string TableType { get; set; } = "BASE TABLE";

    /// <summary>
    /// Gets or sets the estimated row count reported by MySQL metadata.
    /// </summary>
    public long EstimatedRowCount { get; set; }

    /// <summary>
    /// Gets the columns belonging to the table.
    /// </summary>
    public List<TableColumnMetadata> Columns { get; } = [];

    /// <summary>
    /// Gets the outgoing foreign-key relationships of the table.
    /// </summary>
    public List<TableForeignKeyMetadata> OutgoingForeignKeys { get; } = [];

    /// <summary>
    /// Gets the incoming foreign-key relationships that point to the table.
    /// </summary>
    public List<TableForeignKeyMetadata> IncomingForeignKeys { get; } = [];
}
