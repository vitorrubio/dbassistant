using MySqlConnector;

namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Generates a Git-friendly RAG artifact from the live database schema.
/// </summary>
public sealed class SchemaKnowledgeGenerator
{
    private readonly SchemaKnowledgeCorpusBuilder _corpusBuilder = new();
    private readonly SchemaKnowledgeArtifactWriter _artifactWriter = new();
    private readonly SchemaKnowledgeEmbeddingsBuilder _embeddingsBuilder = new();

    /// <summary>
    /// Connects to the source database, reads schema metadata, and writes the generated knowledge artifact to disk.
    /// </summary>
    /// <param name="options">The options that describe the source database and output path.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the generation process.</param>
    /// <returns>The paths of the generated artifacts.</returns>
    public async Task<KnowledgeGenerationResult> GenerateAsync(KnowledgeGenerationOptions options, CancellationToken cancellationToken)
    {
        var schemaMetadata = await ReadTableMetadataAsync(options, cancellationToken);
        Directory.CreateDirectory(options.OutputDirectory);

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var (artifact, embeddingRecords) = _corpusBuilder.Build(
            options.Database,
            schemaMetadata.SchemaName,
            schemaMetadata.Tables,
            generatedAtUtc);
        var embeddingsArtifact = options.CanGenerateEmbeddings()
            ? await _embeddingsBuilder.BuildAsync(options, artifact, cancellationToken)
            : null;

        return await _artifactWriter.WriteAsync(options, artifact, embeddingRecords, embeddingsArtifact, cancellationToken);
    }

    private static async Task<SchemaMetadataSnapshot> ReadTableMetadataAsync(
        KnowledgeGenerationOptions options,
        CancellationToken cancellationToken)
    {
        const string schemaResolutionSql = """
            SELECT SCHEMA_NAME
            FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE LOWER(SCHEMA_NAME) = LOWER(@schemaName)
            LIMIT 1;
            """;

        const string tablesSql = """
            SELECT TABLE_NAME, TABLE_TYPE, COALESCE(TABLE_ROWS, 0) AS TABLE_ROWS
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schemaName
            ORDER BY TABLE_NAME;
            """;

        const string columnsSql = """
            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_KEY
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schemaName
            ORDER BY TABLE_NAME, ORDINAL_POSITION;
            """;

        const string foreignKeysSql = """
            SELECT
                CONSTRAINT_NAME,
                TABLE_NAME,
                COLUMN_NAME,
                REFERENCED_TABLE_NAME,
                REFERENCED_COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = @schemaName
              AND REFERENCED_TABLE_NAME IS NOT NULL
            ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;
            """;

        var tables = new Dictionary<string, TableMetadata>(StringComparer.OrdinalIgnoreCase);

        await using var connection = new MySqlConnection(options.BuildServerConnectionString());
        await connection.OpenAsync(cancellationToken);
        var resolvedSchemaName = options.Database;

        await using (var command = new MySqlCommand(schemaResolutionSql, connection))
        {
            command.Parameters.AddWithValue("@schemaName", options.Database);
            var schemaName = await command.ExecuteScalarAsync(cancellationToken);

            if (schemaName is string resolvedValue && string.IsNullOrWhiteSpace(resolvedValue) is false)
            {
                resolvedSchemaName = resolvedValue;
            }
        }

        await using (var command = new MySqlCommand(tablesSql, connection))
        {
            command.Parameters.AddWithValue("@schemaName", resolvedSchemaName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var table = new TableMetadata
                {
                    TableName = reader.GetString("TABLE_NAME"),
                    TableType = reader.GetString("TABLE_TYPE"),
                    EstimatedRowCount = reader.GetInt64("TABLE_ROWS")
                };

                tables[table.TableName] = table;
            }
        }

        await using (var command = new MySqlCommand(columnsSql, connection))
        {
            command.Parameters.AddWithValue("@schemaName", resolvedSchemaName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var tableName = reader.GetString("TABLE_NAME");

                if (tables.TryGetValue(tableName, out var table) is false)
                {
                    continue;
                }

                table.Columns.Add(new TableColumnMetadata
                {
                    TableName = tableName,
                    ColumnName = reader.GetString("COLUMN_NAME"),
                    DataType = reader.GetString("DATA_TYPE"),
                    IsNullable = string.Equals(reader.GetString("IS_NULLABLE"), "YES", StringComparison.OrdinalIgnoreCase),
                    ColumnKey = reader.GetString("COLUMN_KEY")
                });
            }
        }

        await using (var command = new MySqlCommand(foreignKeysSql, connection))
        {
            command.Parameters.AddWithValue("@schemaName", resolvedSchemaName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var foreignKey = new TableForeignKeyMetadata
                {
                    ConstraintName = reader.GetString("CONSTRAINT_NAME"),
                    TableName = reader.GetString("TABLE_NAME"),
                    ColumnName = reader.GetString("COLUMN_NAME"),
                    ReferencedTableName = reader.GetString("REFERENCED_TABLE_NAME"),
                    ReferencedColumnName = reader.GetString("REFERENCED_COLUMN_NAME")
                };

                if (tables.TryGetValue(foreignKey.TableName, out var sourceTable))
                {
                    sourceTable.OutgoingForeignKeys.Add(foreignKey);
                }

                if (tables.TryGetValue(foreignKey.ReferencedTableName, out var referencedTable))
                {
                    referencedTable.IncomingForeignKeys.Add(foreignKey);
                }
            }
        }

        return new SchemaMetadataSnapshot(
            resolvedSchemaName,
            tables.Values.OrderBy(table => table.TableName, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private sealed record SchemaMetadataSnapshot(string SchemaName, IReadOnlyCollection<TableMetadata> Tables);
}
