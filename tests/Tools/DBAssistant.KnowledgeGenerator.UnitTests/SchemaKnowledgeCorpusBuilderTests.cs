using DBAssistant.KnowledgeGenerator;
using FluentAssertions;
using Xunit;

namespace DBAssistant.KnowledgeGenerator.UnitTests;

public sealed class SchemaKnowledgeCorpusBuilderTests
{
    [Fact]
    public void Build_ShouldGenerateOverviewTableRelationshipAndJoinPathDocuments()
    {
        var builder = new SchemaKnowledgeCorpusBuilder();
        var generatedAtUtc = new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero);

        var (artifact, embeddingRecords) = builder.Build("Northwind", "northwind", BuildNorthwindLikeTables(), generatedAtUtc);

        artifact.Documents.Should().Contain(document => document.DocType == "database_overview");
        artifact.Documents.Should().Contain(document => document.Id == "table:orders" && document.DocType == "fact_table");
        artifact.Documents.Should().Contain(document => document.Id == "table:order_details" && document.DocType == "fact_table");
        artifact.Documents.Should().Contain(document => document.Id == "table:orders_status" && document.DocType == "lookup_table");
        artifact.Documents.Should().Contain(document => document.Id == "relationship:orders.customer_id->customers.id");
        artifact.Documents.Should().Contain(document => document.Id == "join_path:customers->orders->order_details->products");

        artifact.Documents.All(document =>
                !string.IsNullOrWhiteSpace(document.EmbeddingInput) &&
                document.TokenEstimate > 0 &&
                !string.IsNullOrWhiteSpace(document.ContentHash) &&
                document.QuestionPatterns.Count > 0 &&
                document.SemanticTags.Count > 0)
            .Should()
            .BeTrue();

        embeddingRecords.Should().HaveCount(artifact.Documents.Count);
        embeddingRecords.All(record => !string.IsNullOrWhiteSpace(record.EmbeddingInput))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Build_ShouldChangeContentHashWhenRelevantMetadataChanges()
    {
        var builder = new SchemaKnowledgeCorpusBuilder();
        var generatedAtUtc = new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero);
        var baselineTables = BuildNorthwindLikeTables();
        var changedTables = BuildNorthwindLikeTables();
        changedTables.Single(table => table.TableName == "orders").Columns.Add(new TableColumnMetadata
        {
            TableName = "orders",
            ColumnName = "amount_due",
            DataType = "decimal",
            ColumnKey = string.Empty
        });

        var (baselineArtifact, _) = builder.Build("Northwind", "northwind", baselineTables, generatedAtUtc);
        var (changedArtifact, _) = builder.Build("Northwind", "northwind", changedTables, generatedAtUtc);

        var baselineHash = baselineArtifact.Documents.Single(document => document.Id == "table:orders").ContentHash;
        var changedHash = changedArtifact.Documents.Single(document => document.Id == "table:orders").ContentHash;

        changedHash.Should().NotBe(baselineHash);
    }

    private static List<TableMetadata> BuildNorthwindLikeTables()
    {
        var customers = new TableMetadata
        {
            TableName = "customers",
            TableType = "BASE TABLE",
            EstimatedRowCount = 120
        };
        customers.Columns.AddRange(
        [
            new TableColumnMetadata { TableName = "customers", ColumnName = "id", DataType = "int", ColumnKey = "PRI" },
            new TableColumnMetadata { TableName = "customers", ColumnName = "company_name", DataType = "varchar" },
            new TableColumnMetadata { TableName = "customers", ColumnName = "city", DataType = "varchar" }
        ]);

        var orders = new TableMetadata
        {
            TableName = "orders",
            TableType = "BASE TABLE",
            EstimatedRowCount = 1000
        };
        orders.Columns.AddRange(
        [
            new TableColumnMetadata { TableName = "orders", ColumnName = "id", DataType = "int", ColumnKey = "PRI" },
            new TableColumnMetadata { TableName = "orders", ColumnName = "customer_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "orders", ColumnName = "employee_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "orders", ColumnName = "shipper_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "orders", ColumnName = "status_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "orders", ColumnName = "order_date", DataType = "datetime" },
            new TableColumnMetadata { TableName = "orders", ColumnName = "shipping_fee", DataType = "decimal" }
        ]);

        var orderDetails = new TableMetadata
        {
            TableName = "order_details",
            TableType = "BASE TABLE",
            EstimatedRowCount = 3000
        };
        orderDetails.Columns.AddRange(
        [
            new TableColumnMetadata { TableName = "order_details", ColumnName = "id", DataType = "int", ColumnKey = "PRI" },
            new TableColumnMetadata { TableName = "order_details", ColumnName = "order_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "order_details", ColumnName = "product_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "order_details", ColumnName = "quantity", DataType = "int" },
            new TableColumnMetadata { TableName = "order_details", ColumnName = "unit_price", DataType = "decimal" },
            new TableColumnMetadata { TableName = "order_details", ColumnName = "discount", DataType = "decimal" }
        ]);

        var products = new TableMetadata
        {
            TableName = "products",
            TableType = "BASE TABLE",
            EstimatedRowCount = 80
        };
        products.Columns.AddRange(
        [
            new TableColumnMetadata { TableName = "products", ColumnName = "id", DataType = "int", ColumnKey = "PRI" },
            new TableColumnMetadata { TableName = "products", ColumnName = "product_name", DataType = "varchar" },
            new TableColumnMetadata { TableName = "products", ColumnName = "supplier_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "products", ColumnName = "category_id", DataType = "int", ColumnKey = "MUL" },
            new TableColumnMetadata { TableName = "products", ColumnName = "list_price", DataType = "decimal" }
        ]);

        var ordersStatus = new TableMetadata
        {
            TableName = "orders_status",
            TableType = "BASE TABLE",
            EstimatedRowCount = 8
        };
        ordersStatus.Columns.AddRange(
        [
            new TableColumnMetadata { TableName = "orders_status", ColumnName = "id", DataType = "int", ColumnKey = "PRI" },
            new TableColumnMetadata { TableName = "orders_status", ColumnName = "status_name", DataType = "varchar" }
        ]);

        orders.OutgoingForeignKeys.AddRange(
        [
            new TableForeignKeyMetadata { ConstraintName = "fk_orders_customers", TableName = "orders", ColumnName = "customer_id", ReferencedTableName = "customers", ReferencedColumnName = "id" },
            new TableForeignKeyMetadata { ConstraintName = "fk_orders_status", TableName = "orders", ColumnName = "status_id", ReferencedTableName = "orders_status", ReferencedColumnName = "id" }
        ]);
        customers.IncomingForeignKeys.Add(new TableForeignKeyMetadata { ConstraintName = "fk_orders_customers", TableName = "orders", ColumnName = "customer_id", ReferencedTableName = "customers", ReferencedColumnName = "id" });
        ordersStatus.IncomingForeignKeys.Add(new TableForeignKeyMetadata { ConstraintName = "fk_orders_status", TableName = "orders", ColumnName = "status_id", ReferencedTableName = "orders_status", ReferencedColumnName = "id" });

        orderDetails.OutgoingForeignKeys.AddRange(
        [
            new TableForeignKeyMetadata { ConstraintName = "fk_order_details_orders", TableName = "order_details", ColumnName = "order_id", ReferencedTableName = "orders", ReferencedColumnName = "id" },
            new TableForeignKeyMetadata { ConstraintName = "fk_order_details_products", TableName = "order_details", ColumnName = "product_id", ReferencedTableName = "products", ReferencedColumnName = "id" }
        ]);
        orders.IncomingForeignKeys.Add(new TableForeignKeyMetadata { ConstraintName = "fk_order_details_orders", TableName = "order_details", ColumnName = "order_id", ReferencedTableName = "orders", ReferencedColumnName = "id" });
        products.IncomingForeignKeys.Add(new TableForeignKeyMetadata { ConstraintName = "fk_order_details_products", TableName = "order_details", ColumnName = "product_id", ReferencedTableName = "products", ReferencedColumnName = "id" });

        return [customers, orders, orderDetails, products, ordersStatus];
    }
}
