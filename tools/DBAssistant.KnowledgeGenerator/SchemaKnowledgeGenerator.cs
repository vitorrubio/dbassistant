using System.Text;
using System.Text.Json;
using MySqlConnector;

namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Generates a Git-friendly RAG artifact from the live database schema.
/// </summary>
public sealed class SchemaKnowledgeGenerator
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Connects to the source database, reads schema metadata, and writes the generated knowledge artifact to disk.
    /// </summary>
    /// <param name="options">The options that describe the source database and output path.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the generation process.</param>
    public async Task GenerateAsync(KnowledgeGenerationOptions options, CancellationToken cancellationToken)
    {
        var tables = await ReadTableMetadataAsync(options, cancellationToken);
        var artifact = new SchemaKnowledgeArtifact
        {
            DatabaseName = options.Database,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Documents = BuildDocuments(tables)
        };

        var outputDirectory = Path.GetDirectoryName(options.OutputPath);

        if (string.IsNullOrWhiteSpace(outputDirectory) is false)
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await File.WriteAllTextAsync(
            options.OutputPath,
            JsonSerializer.Serialize(artifact, JsonSerializerOptions),
            cancellationToken);
    }

    private static async Task<IReadOnlyCollection<TableMetadata>> ReadTableMetadataAsync(
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
            SELECT TABLE_NAME, COALESCE(TABLE_ROWS, 0) AS TABLE_ROWS
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schemaName
              AND TABLE_TYPE = 'BASE TABLE'
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

        return tables.Values.OrderBy(table => table.TableName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyCollection<SchemaKnowledgeDocument> BuildDocuments(IReadOnlyCollection<TableMetadata> tables)
    {
        var documents = tables
            .Select(BuildTableDocument)
            .ToList();

        documents.Add(BuildRelationshipDocument(tables));

        return documents;
    }

    private static SchemaKnowledgeDocument BuildTableDocument(TableMetadata table)
    {
        var primaryKeys = table.Columns
            .Where(column => string.Equals(column.ColumnKey, "PRI", StringComparison.OrdinalIgnoreCase))
            .Select(column => column.ColumnName)
            .ToArray();

        var numericColumns = table.Columns
            .Where(column => IsNumericType(column.DataType))
            .Select(column => column.ColumnName)
            .ToArray();

        var dateColumns = table.Columns
            .Where(column => IsDateLikeType(column.DataType))
            .Select(column => column.ColumnName)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine($"Table {table.TableName} is available in the connected database.");
        builder.AppendLine($"Estimated rows: {table.EstimatedRowCount}.");
        builder.AppendLine(primaryKeys.Length == 0
            ? "Primary key: none detected from metadata."
            : $"Primary key columns: {string.Join(", ", primaryKeys)}.");

        if (dateColumns.Length > 0)
        {
            builder.AppendLine($"Date columns useful for time filters: {string.Join(", ", dateColumns)}.");
        }

        if (numericColumns.Length > 0)
        {
            builder.AppendLine($"Numeric columns useful for aggregations: {string.Join(", ", numericColumns)}.");
        }

        builder.AppendLine("Columns:");

        foreach (var column in table.Columns)
        {
            builder.AppendLine(
                $"- {column.ColumnName} ({column.DataType}, {(column.IsNullable ? "nullable" : "required")}, key: {(string.IsNullOrWhiteSpace(column.ColumnKey) ? "none" : column.ColumnKey)}).");
        }

        builder.AppendLine("Outgoing relationships:");

        if (table.OutgoingForeignKeys.Count == 0)
        {
            builder.AppendLine("- none.");
        }
        else
        {
            foreach (var relationship in table.OutgoingForeignKeys)
            {
                builder.AppendLine(
                    $"- {relationship.TableName}.{relationship.ColumnName} -> {relationship.ReferencedTableName}.{relationship.ReferencedColumnName}.");
            }
        }

        builder.AppendLine("Incoming relationships:");

        if (table.IncomingForeignKeys.Count == 0)
        {
            builder.AppendLine("- none.");
        }
        else
        {
            foreach (var relationship in table.IncomingForeignKeys)
            {
                builder.AppendLine(
                    $"- {relationship.TableName}.{relationship.ColumnName} references {relationship.ReferencedTableName}.{relationship.ReferencedColumnName}.");
            }
        }

        return new SchemaKnowledgeDocument
        {
            Id = $"table:{table.TableName.ToLowerInvariant()}",
            Title = $"{table.TableName} table reference",
            Content = builder.ToString().TrimEnd(),
            TableNames = BuildTableNames(table),
            Keywords = BuildKeywords(table)
        };
    }

    private static SchemaKnowledgeDocument BuildRelationshipDocument(IReadOnlyCollection<TableMetadata> tables)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Relationship overview for the connected database:");

        foreach (var table in tables.Where(table => table.OutgoingForeignKeys.Count > 0))
        {
            builder.AppendLine($"{table.TableName}:");

            foreach (var relationship in table.OutgoingForeignKeys)
            {
                builder.AppendLine(
                    $"- join {relationship.TableName}.{relationship.ColumnName} with {relationship.ReferencedTableName}.{relationship.ReferencedColumnName}.");
            }
        }

        return new SchemaKnowledgeDocument
        {
            Id = "relationships:overview",
            Title = "Database relationship overview",
            Content = builder.ToString().TrimEnd(),
            TableNames = tables.Select(table => table.TableName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Keywords = tables
                .SelectMany(table => SplitTerms(table.TableName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static IReadOnlyCollection<string> BuildTableNames(TableMetadata table)
    {
        return table.OutgoingForeignKeys
            .Select(relationship => relationship.ReferencedTableName)
            .Append(table.TableName)
            .Concat(table.IncomingForeignKeys.Select(relationship => relationship.TableName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyCollection<string> BuildKeywords(TableMetadata table)
    {
        return table.Columns
            .Select(column => column.ColumnName)
            .Concat(table.OutgoingForeignKeys.Select(relationship => relationship.ReferencedTableName))
            .Concat(table.IncomingForeignKeys.Select(relationship => relationship.TableName))
            .Append(table.TableName)
            .SelectMany(SplitTerms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> SplitTerms(string value)
    {
        return value
            .Replace("_", " ", StringComparison.Ordinal)
            .Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(term => SplitPascalCase(term))
            .Where(term => term.Length >= 2);
    }

    private static IEnumerable<string> SplitPascalCase(string value)
    {
        var current = new StringBuilder();

        foreach (var character in value)
        {
            if (current.Length > 0 && char.IsUpper(character) && char.IsLower(current[^1]))
            {
                yield return current.ToString();
                current.Clear();
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static bool IsNumericType(string dataType)
    {
        return dataType.Contains("int", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("decimal", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("float", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("double", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDateLikeType(string dataType)
    {
        return dataType.Contains("date", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("time", StringComparison.OrdinalIgnoreCase) ||
               dataType.Contains("year", StringComparison.OrdinalIgnoreCase);
    }
}
