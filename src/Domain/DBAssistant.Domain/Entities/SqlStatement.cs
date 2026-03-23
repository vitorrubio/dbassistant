using DBAssistant.Domain.Common;
using DBAssistant.Domain.Exceptions;

namespace DBAssistant.Domain.Entities;

public sealed partial class SqlStatement
{
    private static readonly string[] AllowedStartingKeywords =
    [
        SqlPolicy.SELECT_KEYWORD,
        SqlPolicy.WITH_KEYWORD
    ];

    private static readonly string[] ForbiddenKeywords =
    [
        "ALTER",
        "CALL",
        "CREATE",
        "DELETE",
        "DROP",
        "EXEC",
        "GRANT",
        "INSERT",
        "MERGE",
        "REPLACE",
        "REVOKE",
        "TRUNCATE",
        "UPDATE"
    ];

    private SqlStatement(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SqlStatement CreateReadOnly(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new DomainValidationException("The generated SQL cannot be empty.");
        }

        var normalizedSql = sql.Trim();
        var firstKeyword = normalizedSql
            .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .ToUpperInvariant();

        if (firstKeyword is null || AllowedStartingKeywords.Contains(firstKeyword) is false)
        {
            throw new DomainValidationException("Only SELECT statements and CTE-based read-only queries are allowed.");
        }

        foreach (var forbiddenKeyword in ForbiddenKeywords)
        {
            if (ContainsForbiddenKeyword(normalizedSql, forbiddenKeyword))
            {
                throw new DomainValidationException($"Forbidden SQL keyword detected: {forbiddenKeyword}.");
            }
        }

        return new SqlStatement(normalizedSql);
    }

    private static bool ContainsForbiddenKeyword(string sql, string keyword)
    {
        return sql.Contains($" {keyword} ", StringComparison.OrdinalIgnoreCase) ||
               sql.Contains($"\n{keyword} ", StringComparison.OrdinalIgnoreCase) ||
               sql.Contains($"\t{keyword} ", StringComparison.OrdinalIgnoreCase) ||
               sql.StartsWith($"{keyword} ", StringComparison.OrdinalIgnoreCase) ||
               sql.EndsWith($" {keyword}", StringComparison.OrdinalIgnoreCase) ||
               sql.Contains($"({keyword} ", StringComparison.OrdinalIgnoreCase);
    }
}
