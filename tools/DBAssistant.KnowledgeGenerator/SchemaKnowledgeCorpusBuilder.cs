namespace DBAssistant.KnowledgeGenerator;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Builds retrieval-oriented schema documents from discovered database metadata.
/// </summary>
public sealed class SchemaKnowledgeCorpusBuilder
{
    private const string MetadataSource = "information_schema.tables+columns+key_column_usage";
    private static readonly string[] MonetaryMarkers = ["price", "amount", "total", "cost", "tax", "fee", "freight", "revenue", "discount", "subtotal"];
    private static readonly string[] QuantityMarkers = ["quantity", "qty", "count", "units", "stock"];
    private static readonly string[] DateMarkers = ["date", "time", "created", "updated", "shipped", "paid", "due"];
    private static readonly string[] StatusMarkers = ["status", "state", "stage", "type", "category"];
    private static readonly string[] DescriptionMarkers = ["name", "title", "description", "notes", "city", "country", "region"];
    private static readonly string[] SalesMarkers = ["order", "invoice", "sale", "sales", "payment", "customer"];
    private static readonly string[] InventoryMarkers = ["product", "inventory", "stock", "supplier", "warehouse", "category"];

    /// <summary>
    /// Builds the schema knowledge artifact and its JSONL embedding companion.
    /// </summary>
    /// <param name="databaseName">The source database name.</param>
    /// <param name="schemaName">The resolved schema name.</param>
    /// <param name="tables">The discovered tables and views.</param>
    /// <param name="generatedAtUtc">The extraction timestamp.</param>
    /// <returns>The artifact plus the JSONL embedding records.</returns>
    public (SchemaKnowledgeArtifact Artifact, IReadOnlyCollection<SchemaEmbeddingInputRecord> EmbeddingRecords) Build(
        string databaseName,
        string schemaName,
        IReadOnlyCollection<TableMetadata> tables,
        DateTimeOffset generatedAtUtc)
    {
        var documents = new List<SchemaKnowledgeDocument>();
        var tableProfiles = tables.ToDictionary(table => table.TableName, CreateTableProfile, StringComparer.OrdinalIgnoreCase);
        var foreignKeys = tables.SelectMany(table => table.OutgoingForeignKeys).ToArray();

        documents.Add(BuildDatabaseOverviewDocument(databaseName, schemaName, tables, tableProfiles, generatedAtUtc));
        documents.AddRange(tables.Select(table => BuildTableDocument(databaseName, schemaName, table, tableProfiles, generatedAtUtc)));
        documents.AddRange(foreignKeys.Select(foreignKey => BuildRelationshipDocument(databaseName, schemaName, foreignKey, tableProfiles, generatedAtUtc)));
        documents.AddRange(BuildJoinPathDocuments(databaseName, schemaName, tables, tableProfiles, generatedAtUtc));

        var artifact = new SchemaKnowledgeArtifact
        {
            DatabaseName = databaseName,
            SchemaName = schemaName,
            GeneratedAtUtc = generatedAtUtc,
            DocumentCount = documents.Count,
            Documents = documents
        };

        var embeddingRecords = documents
            .Select(document => new SchemaEmbeddingInputRecord
            {
                Id = document.Id,
                DocType = document.DocType,
                DatabaseName = document.DatabaseName,
                SchemaName = document.SchemaName,
                TableName = document.TableName,
                RelatedTables = document.RelatedTables,
                Title = document.Title,
                EmbeddingInput = document.EmbeddingInput,
                ContentHash = document.ContentHash,
                TokenEstimate = document.TokenEstimate
            })
            .ToArray();

        return (artifact, embeddingRecords);
    }

    private static SchemaKnowledgeDocument BuildDatabaseOverviewDocument(
        string databaseName,
        string schemaName,
        IReadOnlyCollection<TableMetadata> tables,
        IReadOnlyDictionary<string, TableProfile> tableProfiles,
        DateTimeOffset generatedAtUtc)
    {
        var coreTables = tableProfiles.Values
            .OrderByDescending(profile => profile.RelatedTables.Count)
            .ThenByDescending(profile => profile.Table.EstimatedRowCount)
            .Take(6)
            .Select(profile => profile.Table.TableName)
            .ToArray();

        var salesTables = tableProfiles.Values.Where(profile => profile.SemanticTags.Contains("sales", StringComparer.OrdinalIgnoreCase)).Select(profile => profile.Table.TableName).ToArray();
        var inventoryTables = tableProfiles.Values.Where(profile => profile.SemanticTags.Contains("inventory", StringComparer.OrdinalIgnoreCase)).Select(profile => profile.Table.TableName).ToArray();
        var lookupTables = tableProfiles.Values.Where(profile => profile.DocType == "lookup_table").Select(profile => profile.Table.TableName).ToArray();

        var joinPaths = BuildImportantPathStrings(tables, tableProfiles)
            .Take(6)
            .ToArray();

        var content = $"""
            {databaseName} uses {tables.Count} schema objects. Core query planning should focus on {ToSentence(coreTables)}.
            The main analytical flow starts from customer or order entities, reaches transactional rows, and then expands to products, employees, shippers, and status lookups through foreign keys.
            Sales-oriented questions are usually answered with {ToSentence(salesTables)}. Inventory or catalog-oriented questions are usually answered with {ToSentence(inventoryTables)}. Lookup and status interpretation typically comes from {ToSentence(lookupTables)}.
            Important join paths include {ToSentence(joinPaths)}.
            Use this overview when deciding which table family answers questions about customers, orders, products, shipping, status, revenue, quantities, and time-based trends.
            """;

        return FinalizeDocument(new SchemaKnowledgeDocument
        {
            Id = "database_overview",
            DocType = "database_overview",
            DatabaseName = databaseName,
            SchemaName = schemaName,
            TableName = string.Empty,
            RelatedTables = coreTables,
            Title = $"{databaseName} database overview",
            Content = content,
            Keywords = coreTables
                .Concat(salesTables)
                .Concat(inventoryTables)
                .Concat(["database", "overview", "schema", "joins"])
                .SelectMany(SplitTerms)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            JoinHints = joinPaths,
            QuestionPatterns =
            [
                "Which tables answer sales by customer and by period questions?",
                "How do I join customers, orders, order details, and products?",
                "Which schema areas answer shipping status and revenue questions?"
            ],
            SemanticTags = ["entity", "sales", "inventory", "lookup"],
            Source = MetadataSource,
            LastExtractedAtUtc = generatedAtUtc
        });
    }

    private static SchemaKnowledgeDocument BuildTableDocument(
        string databaseName,
        string schemaName,
        TableMetadata table,
        IReadOnlyDictionary<string, TableProfile> tableProfiles,
        DateTimeOffset generatedAtUtc)
    {
        var profile = tableProfiles[table.TableName];
        var primaryKeys = table.Columns.Where(column => column.ColumnKey.Equals("PRI", StringComparison.OrdinalIgnoreCase)).Select(column => column.ColumnName).ToArray();
        var foreignKeys = table.OutgoingForeignKeys.Select(foreignKey => $"{foreignKey.ColumnName} -> {foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName}").ToArray();
        var inboundReferences = table.IncomingForeignKeys.Select(foreignKey => $"{foreignKey.TableName}.{foreignKey.ColumnName}").ToArray();
        var relevantColumns = table.Columns.Select(column => $"{column.ColumnName} ({column.DataType})").ToArray();

        var content = $"""
            {DescribeTablePurpose(profile)}
            Primary key columns: {ToSentence(primaryKeys)}. Foreign keys: {ToSentence(foreignKeys)}. Incoming references: {ToSentence(inboundReferences)}.
            Relevant columns: {ToSentence(relevantColumns)}.
            Date fields: {ToSentence(profile.DateColumns)}. Monetary fields: {ToSentence(profile.MonetaryColumns)}. Quantity fields: {ToSentence(profile.QuantityColumns)}. Descriptive fields: {ToSentence(profile.DescriptiveColumns)}.
            Filter candidates: {ToSentence(profile.FilterCandidates)}. Grouping candidates: {ToSentence(profile.GroupCandidates)}. Sort candidates: {ToSentence(profile.SortCandidates)}. Aggregation candidates: {ToSentence(profile.AggregationCandidates)}.
            Typical joins: {ToSentence(profile.JoinHints)}.
            This table helps answer: {ToSentence(profile.QuestionPatterns)}.
            """;

        return FinalizeDocument(new SchemaKnowledgeDocument
        {
            Id = $"table:{NormalizeName(table.TableName)}",
            DocType = profile.DocType,
            DatabaseName = databaseName,
            SchemaName = schemaName,
            TableName = table.TableName,
            RelatedTables = profile.RelatedTables,
            Title = $"{table.TableName} {profile.DocType.Replace('_', ' ')}",
            Content = content,
            Keywords = profile.Keywords,
            JoinHints = profile.JoinHints,
            QuestionPatterns = profile.QuestionPatterns,
            SemanticTags = profile.SemanticTags,
            Source = MetadataSource,
            LastExtractedAtUtc = generatedAtUtc
        });
    }

    private static SchemaKnowledgeDocument BuildRelationshipDocument(
        string databaseName,
        string schemaName,
        TableForeignKeyMetadata foreignKey,
        IReadOnlyDictionary<string, TableProfile> tableProfiles,
        DateTimeOffset generatedAtUtc)
    {
        var childProfile = tableProfiles[foreignKey.TableName];
        var parentProfile = tableProfiles[foreignKey.ReferencedTableName];
        var relationshipName = $"{foreignKey.TableName}.{foreignKey.ColumnName}->{foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName}";
        var cardinality = childProfile.DocType is "fact_table" or "table" ? "many-to-one" : "reference-to-parent";
        var meaning = $"This relationship connects {foreignKey.TableName} rows to {foreignKey.ReferencedTableName} through {foreignKey.ColumnName}.";
        var content = $"""
            Child table: {foreignKey.TableName}. Child column: {foreignKey.ColumnName}. Parent table: {foreignKey.ReferencedTableName}. Parent column: {foreignKey.ReferencedColumnName}.
            {meaning}
            Typical usage: join {foreignKey.TableName}.{foreignKey.ColumnName} = {foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName} to enrich {foreignKey.TableName} with parent attributes or to filter child facts by parent dimensions.
            Probable cardinality: {cardinality}.
            Common questions: {ToSentence(BuildRelationshipQuestions(foreignKey, childProfile, parentProfile))}.
            """;

        return FinalizeDocument(new SchemaKnowledgeDocument
        {
            Id = $"relationship:{NormalizeName(foreignKey.TableName)}.{NormalizeName(foreignKey.ColumnName)}->{NormalizeName(foreignKey.ReferencedTableName)}.{NormalizeName(foreignKey.ReferencedColumnName)}",
            DocType = "relationship",
            DatabaseName = databaseName,
            SchemaName = schemaName,
            TableName = foreignKey.TableName,
            RelatedTables = [foreignKey.TableName, foreignKey.ReferencedTableName],
            Title = $"{foreignKey.TableName}.{foreignKey.ColumnName} to {foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName}",
            Content = content,
            Keywords = childProfile.Keywords.Concat(parentProfile.Keywords).Concat(SplitTerms(foreignKey.ColumnName)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            JoinHints = [$"{foreignKey.TableName}.{foreignKey.ColumnName} = {foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName}"],
            QuestionPatterns = BuildRelationshipQuestions(foreignKey, childProfile, parentProfile),
            SemanticTags = childProfile.SemanticTags.Concat(parentProfile.SemanticTags).Concat(["relationship"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Source = MetadataSource,
            LastExtractedAtUtc = generatedAtUtc
        });
    }

    private static IReadOnlyCollection<SchemaKnowledgeDocument> BuildJoinPathDocuments(
        string databaseName,
        string schemaName,
        IReadOnlyCollection<TableMetadata> tables,
        IReadOnlyDictionary<string, TableProfile> tableProfiles,
        DateTimeOffset generatedAtUtc)
    {
        var adjacency = BuildAdjacency(tables);
        var documents = new List<SchemaKnowledgeDocument>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in tables.OrderBy(table => table.TableName, StringComparer.OrdinalIgnoreCase))
        {
            var stack = new Stack<List<string>>();
            stack.Push([table.TableName]);

            while (stack.Count > 0)
            {
                var path = stack.Pop();

                if (path.Count >= 2 && path.Count <= 4 && IsImportantPath(path, tableProfiles))
                {
                    var canonicalKey = CanonicalizePath(path);

                    if (seen.Add(canonicalKey))
                    {
                        documents.Add(BuildJoinPathDocument(databaseName, schemaName, path, tables, tableProfiles, generatedAtUtc));
                    }
                }

                if (path.Count == 4)
                {
                    continue;
                }

                var current = path[^1];

                foreach (var next in adjacency.GetValueOrDefault(current, []))
                {
                    if (path.Contains(next, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var nextPath = new List<string>(path) { next };
                    stack.Push(nextPath);
                }
            }
        }

        return documents
            .OrderBy(document => document.RelatedTables.Count)
            .ThenBy(document => document.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SchemaKnowledgeDocument BuildJoinPathDocument(
        string databaseName,
        string schemaName,
        IReadOnlyList<string> path,
        IReadOnlyCollection<TableMetadata> tables,
        IReadOnlyDictionary<string, TableProfile> tableProfiles,
        DateTimeOffset generatedAtUtc)
    {
        var joinHints = path
            .Zip(path.Skip(1))
            .Select(pair => ResolveJoinHint(pair.First, pair.Second, tables))
            .Where(hint => string.IsNullOrWhiteSpace(hint) is false)
            .ToArray();

        var questionPatterns = BuildJoinPathQuestionPatterns(path, tableProfiles);
        var semanticTags = path.SelectMany(tableName => tableProfiles[tableName].SemanticTags).Concat(["join_path"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var content = $"""
            Exact path: {string.Join(" -> ", path)}.
            Use this path when a question starts from {path[0]} and needs attributes or measures from {path[^1]} across intermediate joins.
            Join sequence: {ToSentence(joinHints)}.
            Typical filters: {ToSentence(BuildPathFilterHints(path, tableProfiles))}.
            Typical aggregations: {ToSentence(BuildPathAggregationHints(path, tableProfiles))}.
            Typical questions: {ToSentence(questionPatterns)}.
            """;

        return FinalizeDocument(new SchemaKnowledgeDocument
        {
            Id = $"join_path:{string.Join("->", path.Select(NormalizeName))}",
            DocType = "join_path",
            DatabaseName = databaseName,
            SchemaName = schemaName,
            TableName = path[0],
            RelatedTables = path.ToArray(),
            Title = $"Join path {string.Join(" -> ", path)}",
            Content = content,
            Keywords = path.SelectMany(SplitTerms).Concat(questionPatterns.SelectMany(SplitTerms)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            JoinHints = joinHints,
            QuestionPatterns = questionPatterns,
            SemanticTags = semanticTags,
            Source = MetadataSource,
            LastExtractedAtUtc = generatedAtUtc
        });
    }

    private static TableProfile CreateTableProfile(TableMetadata table)
    {
        var tableNameTerms = SplitTerms(table.TableName).ToArray();
        var dateColumns = table.Columns.Where(column => IsDateColumn(column)).Select(column => column.ColumnName).ToArray();
        var monetaryColumns = table.Columns.Where(column => IsMonetaryColumn(column)).Select(column => column.ColumnName).ToArray();
        var quantityColumns = table.Columns.Where(column => IsQuantityColumn(column)).Select(column => column.ColumnName).ToArray();
        var descriptiveColumns = table.Columns.Where(column => IsDescriptiveColumn(column)).Select(column => column.ColumnName).ToArray();
        var aggregationCandidates = table.Columns.Where(column => IsMetricColumn(column)).Select(column => column.ColumnName).ToArray();
        var filterCandidates = table.Columns.Where(column => IsFilterCandidate(column)).Select(column => column.ColumnName).ToArray();
        var groupCandidates = descriptiveColumns.Concat(table.Columns.Where(column => column.ColumnKey.Equals("PRI", StringComparison.OrdinalIgnoreCase)).Select(column => column.ColumnName)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sortCandidates = dateColumns.Concat(monetaryColumns).Concat(quantityColumns).Concat(descriptiveColumns).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var relatedTables = table.OutgoingForeignKeys.Select(foreignKey => foreignKey.ReferencedTableName)
            .Concat(table.IncomingForeignKeys.Select(foreignKey => foreignKey.TableName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var semanticTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (table.TableType.Equals("VIEW", StringComparison.OrdinalIgnoreCase))
        {
            semanticTags.Add("entity");
        }

        if (tableNameTerms.Any(term => SalesMarkers.Contains(term, StringComparer.OrdinalIgnoreCase)))
        {
            semanticTags.Add("sales");
        }

        if (tableNameTerms.Any(term => InventoryMarkers.Contains(term, StringComparer.OrdinalIgnoreCase)))
        {
            semanticTags.Add("inventory");
        }

        if (dateColumns.Length > 0)
        {
            semanticTags.Add("date");
        }

        if (table.Columns.Any(column => column.DataType.Contains("time", StringComparison.OrdinalIgnoreCase)))
        {
            semanticTags.Add("datetime");
        }

        if (aggregationCandidates.Length > 0)
        {
            semanticTags.Add("metric");
        }

        if (monetaryColumns.Length > 0)
        {
            semanticTags.Add("monetary");
        }

        if (quantityColumns.Length > 0)
        {
            semanticTags.Add("quantity");
        }

        if (tableNameTerms.Any(term => term.Contains("status", StringComparison.OrdinalIgnoreCase)))
        {
            semanticTags.Add("status");
        }

        var isView = table.TableType.Equals("VIEW", StringComparison.OrdinalIgnoreCase);
        var isLookup = tableNameTerms.Any(term => StatusMarkers.Contains(term, StringComparer.OrdinalIgnoreCase)) ||
                       (table.IncomingForeignKeys.Count > 0 && table.OutgoingForeignKeys.Count == 0 && table.Columns.Count <= 8 && aggregationCandidates.Length == 0);
        var isLineItems = tableNameTerms.Any(term => term is "detail" or "details" or "item" or "items" or "line");
        var isBridge = table.OutgoingForeignKeys.Count >= 2 &&
                       table.Columns.Count <= Math.Max(6, table.OutgoingForeignKeys.Count + 3) &&
                       table.Columns.Count(column => column.ColumnKey.Equals("PRI", StringComparison.OrdinalIgnoreCase)) >= 1;
        var isFact = isLineItems ||
                     tableNameTerms.Any(term => term is "order" or "orders" or "invoice" or "sales") ||
                     (aggregationCandidates.Length >= 2 && dateColumns.Length > 0) ||
                     (table.OutgoingForeignKeys.Count >= 2 && aggregationCandidates.Length > 0);

        if (isLookup)
        {
            semanticTags.Add("lookup");
        }
        else
        {
            semanticTags.Add("entity");
        }

        if (isLineItems)
        {
            semanticTags.Add("line_items");
        }

        if (isBridge)
        {
            semanticTags.Add("bridge");
        }

        var docType = isView
            ? "view"
            : isLookup
                ? "lookup_table"
                : isFact
                    ? "fact_table"
                    : "table";

        var keywords = table.Columns
            .Select(column => column.ColumnName)
            .Concat(tableNameTerms)
            .Concat(relatedTables)
            .Concat(BuildAliases(table.TableName))
            .SelectMany(SplitTerms)
            .Concat(semanticTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var joinHints = table.OutgoingForeignKeys.Select(foreignKey => $"{table.TableName}.{foreignKey.ColumnName} = {foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName}")
            .Concat(table.IncomingForeignKeys.Select(foreignKey => $"{foreignKey.TableName}.{foreignKey.ColumnName} = {foreignKey.ReferencedTableName}.{foreignKey.ReferencedColumnName}"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var questionPatterns = BuildTableQuestionPatterns(table.TableName, docType, relatedTables, descriptiveColumns, dateColumns, monetaryColumns, quantityColumns);

        return new TableProfile(
            table,
            docType,
            relatedTables,
            keywords,
            joinHints,
            questionPatterns,
            semanticTags.ToArray(),
            dateColumns,
            monetaryColumns,
            quantityColumns,
            descriptiveColumns,
            filterCandidates,
            groupCandidates,
            sortCandidates,
            aggregationCandidates);
    }

    private static SchemaKnowledgeDocument FinalizeDocument(SchemaKnowledgeDocument document)
    {
        document.RelatedTables = document.RelatedTables
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        document.Keywords = document.Keywords
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        document.JoinHints = document.JoinHints
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        document.QuestionPatterns = document.QuestionPatterns
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        document.SemanticTags = document.SemanticTags
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        document.Content = NormalizeWhitespace(document.Content);
        document.EmbeddingInput = BuildEmbeddingInput(document);
        document.TokenEstimate = EstimateTokens(document.EmbeddingInput);
        document.ContentHash = ComputeContentHash(document);
        return document;
    }

    private static string BuildEmbeddingInput(SchemaKnowledgeDocument document)
    {
        return NormalizeWhitespace(
            $"""
            Document type: {document.DocType}
            Title: {document.Title}
            Database: {document.DatabaseName}
            Schema: {document.SchemaName}
            Primary table: {document.TableName}
            Related tables: {ToSentence(document.RelatedTables)}
            Semantic tags: {ToSentence(document.SemanticTags)}
            Join hints: {ToSentence(document.JoinHints)}
            Question patterns: {ToSentence(document.QuestionPatterns)}
            Content: {document.Content}
            Keywords: {ToSentence(document.Keywords)}
            """);
    }

    private static string ComputeContentHash(SchemaKnowledgeDocument document)
    {
        var relevantContent = string.Join(
            "\n",
            document.DocType,
            document.TableName,
            document.Title,
            document.Content,
            string.Join("|", document.RelatedTables),
            string.Join("|", document.Keywords),
            string.Join("|", document.JoinHints),
            string.Join("|", document.QuestionPatterns),
            string.Join("|", document.SemanticTags),
            document.EmbeddingInput);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(relevantContent));
        return Convert.ToHexString(bytes);
    }

    private static int EstimateTokens(string value)
    {
        return Math.Max(1, (int)Math.Ceiling(value.Length / 4d));
    }

    private static string DescribeTablePurpose(TableProfile profile)
    {
        var tableName = profile.Table.TableName;

        if (profile.DocType == "lookup_table")
        {
            return $"{tableName} is a lookup or status table that adds categorical meaning to related business records.";
        }

        if (profile.SemanticTags.Contains("line_items", StringComparer.OrdinalIgnoreCase))
        {
            return $"{tableName} stores line-level transactional rows and usually connects a business header entity to detailed measures such as quantity, price, or discount.";
        }

        if (profile.DocType == "fact_table")
        {
            return $"{tableName} is a central transactional table used for business activity, metrics, dates, and joins into dimensions or lookup tables.";
        }

        if (profile.DocType == "view")
        {
            return $"{tableName} is a database view that exposes a query-ready projection over underlying schema objects.";
        }

        return $"{tableName} is a business entity table that provides descriptive attributes, filters, and grouping dimensions for analytical queries.";
    }

    private static IReadOnlyCollection<string> BuildTableQuestionPatterns(
        string tableName,
        string docType,
        IReadOnlyCollection<string> relatedTables,
        IReadOnlyCollection<string> descriptiveColumns,
        IReadOnlyCollection<string> dateColumns,
        IReadOnlyCollection<string> monetaryColumns,
        IReadOnlyCollection<string> quantityColumns)
    {
        var normalizedTable = Humanize(tableName);
        var patterns = new List<string>();

        if (docType == "fact_table")
        {
            patterns.Add($"What is the total {normalizedTable} volume by period?");
            patterns.Add($"Which {relatedTables.FirstOrDefault() ?? normalizedTable} drive the highest {normalizedTable} metrics?");
        }

        if (quantityColumns.Count > 0)
        {
            patterns.Add($"What is the total {Humanize(quantityColumns.First())} by {normalizedTable}?");
        }

        if (monetaryColumns.Count > 0)
        {
            patterns.Add($"What is the sum of {Humanize(monetaryColumns.First())} by {normalizedTable}?");
        }

        if (dateColumns.Count > 0)
        {
            patterns.Add($"How does {normalizedTable} change over {Humanize(dateColumns.First())}?");
        }

        if (descriptiveColumns.Count > 0)
        {
            patterns.Add($"Which {Humanize(descriptiveColumns.First())} values are most frequent in {normalizedTable}?");
        }

        if (patterns.Count == 0)
        {
            patterns.Add($"Which records exist in {normalizedTable} and how do they join to related tables?");
        }

        return patterns.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyCollection<string> BuildRelationshipQuestions(TableForeignKeyMetadata foreignKey, TableProfile childProfile, TableProfile parentProfile)
    {
        return
        [
            $"How do I join {Humanize(foreignKey.TableName)} to {Humanize(foreignKey.ReferencedTableName)}?",
            $"Which {Humanize(foreignKey.ReferencedTableName)} attributes filter {Humanize(foreignKey.TableName)} records?",
            $"How do I aggregate {Humanize(foreignKey.TableName)} metrics by {Humanize(foreignKey.ReferencedTableName)}?"
        ];
    }

    private static IReadOnlyCollection<string> BuildJoinPathQuestionPatterns(IReadOnlyList<string> path, IReadOnlyDictionary<string, TableProfile> tableProfiles)
    {
        var start = Humanize(path[0]);
        var end = Humanize(path[^1]);
        var middle = path.Count > 2 ? Humanize(path[1]) : end;

        return
        [
            $"Which {end} records are associated with each {start}?",
            $"How do I analyze {end} metrics through {middle}?",
            $"What filters and aggregations are typical along {string.Join(" -> ", path.Select(Humanize))}?"
        ];
    }

    private static IReadOnlyCollection<string> BuildPathFilterHints(IReadOnlyList<string> path, IReadOnlyDictionary<string, TableProfile> tableProfiles)
    {
        return path
            .SelectMany(tableName => tableProfiles[tableName].FilterCandidates.Take(2))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static IReadOnlyCollection<string> BuildPathAggregationHints(IReadOnlyList<string> path, IReadOnlyDictionary<string, TableProfile> tableProfiles)
    {
        return path
            .SelectMany(tableName => tableProfiles[tableName].AggregationCandidates.Take(2))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static Dictionary<string, List<string>> BuildAdjacency(IReadOnlyCollection<TableMetadata> tables)
    {
        var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in tables)
        {
            adjacency.TryAdd(table.TableName, []);
        }

        foreach (var foreignKey in tables.SelectMany(table => table.OutgoingForeignKeys))
        {
            adjacency[foreignKey.TableName].Add(foreignKey.ReferencedTableName);
            adjacency[foreignKey.ReferencedTableName].Add(foreignKey.TableName);
        }

        return adjacency.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsImportantPath(IReadOnlyList<string> path, IReadOnlyDictionary<string, TableProfile> tableProfiles)
    {
        if (path.Count == 2)
        {
            return true;
        }

        return path.Any(tableName => tableProfiles[tableName].SemanticTags.Contains("sales", StringComparer.OrdinalIgnoreCase)) ||
               path.Any(tableName => tableProfiles[tableName].SemanticTags.Contains("line_items", StringComparer.OrdinalIgnoreCase)) ||
               path.Any(tableName => tableProfiles[tableName].DocType == "lookup_table");
    }

    private static IEnumerable<string> BuildImportantPathStrings(IReadOnlyCollection<TableMetadata> tables, IReadOnlyDictionary<string, TableProfile> tableProfiles)
    {
        return BuildAdjacency(tables)
            .Keys
            .SelectMany(start =>
            {
                var results = new List<string>();
                var stack = new Stack<List<string>>();
                stack.Push([start]);

                while (stack.Count > 0)
                {
                    var path = stack.Pop();

                    if (path.Count >= 2 && path.Count <= 4 && IsImportantPath(path, tableProfiles))
                    {
                        results.Add(string.Join(" -> ", path));
                    }

                    if (path.Count == 4)
                    {
                        continue;
                    }

                    foreach (var next in BuildAdjacency(tables).GetValueOrDefault(path[^1], []))
                    {
                        if (path.Contains(next, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var nextPath = new List<string>(path) { next };
                        stack.Push(nextPath);
                    }
                }

                return results;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveJoinHint(string leftTable, string rightTable, IReadOnlyCollection<TableMetadata> tables)
    {
        var left = tables.First(table => table.TableName.Equals(leftTable, StringComparison.OrdinalIgnoreCase));
        var direct = left.OutgoingForeignKeys.FirstOrDefault(foreignKey => foreignKey.ReferencedTableName.Equals(rightTable, StringComparison.OrdinalIgnoreCase));

        if (direct is not null)
        {
            return $"{leftTable}.{direct.ColumnName} = {rightTable}.{direct.ReferencedColumnName}";
        }

        var right = tables.First(table => table.TableName.Equals(rightTable, StringComparison.OrdinalIgnoreCase));
        var reverse = right.OutgoingForeignKeys.FirstOrDefault(foreignKey => foreignKey.ReferencedTableName.Equals(leftTable, StringComparison.OrdinalIgnoreCase));

        return reverse is null
            ? string.Empty
            : $"{rightTable}.{reverse.ColumnName} = {leftTable}.{reverse.ReferencedColumnName}";
    }

    private static IReadOnlyCollection<string> BuildAliases(string tableName)
    {
        var aliases = new List<string>();
        var normalized = NormalizeName(tableName);

        if (normalized is "customers")
        {
            aliases.AddRange(["customer", "client"]);
        }

        if (normalized is "products")
        {
            aliases.AddRange(["product", "item"]);
        }

        if (normalized is "orders")
        {
            aliases.AddRange(["order", "purchase"]);
        }

        if (normalized.Contains("date", StringComparison.OrdinalIgnoreCase))
        {
            aliases.Add("business date");
        }

        return aliases;
    }

    private static bool IsMetricColumn(TableColumnMetadata column)
    {
        return IsNumericColumn(column) &&
               column.ColumnKey.Equals("PRI", StringComparison.OrdinalIgnoreCase) is false &&
               column.ColumnKey.Equals("MUL", StringComparison.OrdinalIgnoreCase) is false;
    }

    private static bool IsNumericColumn(TableColumnMetadata column)
    {
        return column.DataType.Contains("int", StringComparison.OrdinalIgnoreCase) ||
               column.DataType.Contains("decimal", StringComparison.OrdinalIgnoreCase) ||
               column.DataType.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
               column.DataType.Contains("float", StringComparison.OrdinalIgnoreCase) ||
               column.DataType.Contains("double", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDateColumn(TableColumnMetadata column)
    {
        return column.DataType.Contains("date", StringComparison.OrdinalIgnoreCase) ||
               column.DataType.Contains("time", StringComparison.OrdinalIgnoreCase) ||
               DateMarkers.Any(marker => column.ColumnName.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMonetaryColumn(TableColumnMetadata column)
    {
        return IsNumericColumn(column) &&
               MonetaryMarkers.Any(marker => column.ColumnName.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsQuantityColumn(TableColumnMetadata column)
    {
        return IsNumericColumn(column) &&
               QuantityMarkers.Any(marker => column.ColumnName.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDescriptiveColumn(TableColumnMetadata column)
    {
        return column.DataType.Contains("char", StringComparison.OrdinalIgnoreCase) ||
               column.DataType.Contains("text", StringComparison.OrdinalIgnoreCase) ||
               DescriptionMarkers.Any(marker => column.ColumnName.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFilterCandidate(TableColumnMetadata column)
    {
        return IsDateColumn(column) ||
               IsMetricColumn(column) ||
               IsDescriptiveColumn(column) ||
               column.ColumnKey.Equals("PRI", StringComparison.OrdinalIgnoreCase) ||
               column.ColumnKey.Equals("MUL", StringComparison.OrdinalIgnoreCase);
    }

    private static string CanonicalizePath(IReadOnlyList<string> path)
    {
        var forward = string.Join("->", path.Select(NormalizeName));
        var reverse = string.Join("->", path.Reverse().Select(NormalizeName));
        return string.CompareOrdinal(forward, reverse) <= 0 ? forward : reverse;
    }

    private static string NormalizeName(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    private static string Humanize(string value)
    {
        return string.Join(" ", SplitTerms(value)).ToLowerInvariant();
    }

    private static IEnumerable<string> SplitTerms(string value)
    {
        return value
            .Replace("_", " ", StringComparison.Ordinal)
            .Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(SplitPascalCase)
            .Select(term => term.Trim())
            .Where(term => term.Length >= 2)
            .Select(term => term.ToLowerInvariant());
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

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(
            Environment.NewLine,
            value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string ToSentence(IEnumerable<string> values)
    {
        var items = values
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return items.Length == 0
            ? "none detected"
            : string.Join(", ", items);
    }

    private sealed record TableProfile(
        TableMetadata Table,
        string DocType,
        IReadOnlyCollection<string> RelatedTables,
        IReadOnlyCollection<string> Keywords,
        IReadOnlyCollection<string> JoinHints,
        IReadOnlyCollection<string> QuestionPatterns,
        IReadOnlyCollection<string> SemanticTags,
        IReadOnlyCollection<string> DateColumns,
        IReadOnlyCollection<string> MonetaryColumns,
        IReadOnlyCollection<string> QuantityColumns,
        IReadOnlyCollection<string> DescriptiveColumns,
        IReadOnlyCollection<string> FilterCandidates,
        IReadOnlyCollection<string> GroupCandidates,
        IReadOnlyCollection<string> SortCandidates,
        IReadOnlyCollection<string> AggregationCandidates);
}
