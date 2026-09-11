using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace StarterKit.Persistence.Extensions;

/// <summary>
/// Provider-agnostic inspection of <see cref="DbUpdateException"/> so handlers can translate an
/// expected constraint race into a friendly result instead of letting it bubble as a 500.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// Returns true when the exception was caused by a unique (or primary-key) constraint
    /// violation on SQL Server, PostgreSQL or SQLite.
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            SqlException sql => sql.Number is 2601 or 2627,
            PostgresException postgres => postgres.SqlState == "23505",
            // 2067 = SQLITE_CONSTRAINT_UNIQUE, 1555 = SQLITE_CONSTRAINT_PRIMARYKEY
            SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 2067 or 1555,
            _ => false,
        };
    }

    /// <summary>
    /// Returns true when the exception was caused by a foreign-key constraint violation
    /// (e.g. deleting a row still referenced by another) on SQL Server, PostgreSQL or SQLite.
    /// </summary>
    public static bool IsForeignKeyConstraintViolation(this DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            SqlException sql => sql.Number is 547,
            PostgresException postgres => postgres.SqlState == "23503",
            // 787 = SQLITE_CONSTRAINT_FOREIGNKEY
            SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 787,
            _ => false,
        };
    }
}
