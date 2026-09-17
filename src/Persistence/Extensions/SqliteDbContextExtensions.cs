using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StarterKit.Persistence.Extensions;

public static class SqliteDbContextExtensions
{
    /// <summary>
    /// Sqlite does not support orderby DateTimeOffset natively, so we convert it to long (Unix time seconds).
    /// </summary>
    public static void FixSqliteDateTimeOffset(this DatabaseFacade database, ModelBuilder modelBuilder)
    {
        if (database.IsSqlite())
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var dateTimeOffsetProps = entityType
                    .GetProperties()
                    .Where(p =>
                        p.ClrType == typeof(DateTimeOffset)
                        || p.ClrType == typeof(DateTimeOffset?));

                foreach (var property in dateTimeOffsetProps)
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTimeOffset, long>(
                                v => v.ToUnixTimeSeconds(),
                                v => DateTimeOffset.FromUnixTimeSeconds(v)));
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTimeOffset?, long?>(
                                v => v.HasValue ? v.Value.ToUnixTimeSeconds() : null,
                                v => v.HasValue ? DateTimeOffset.FromUnixTimeSeconds(v.Value) : null));
                    }
                }
            }
        }
    }
}
