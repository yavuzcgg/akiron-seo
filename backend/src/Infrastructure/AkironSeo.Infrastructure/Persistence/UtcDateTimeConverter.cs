using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AkironSeo.Infrastructure.Persistence;

/// <summary>
/// Normalizes every persisted <see cref="DateTime"/> to UTC.
/// Npgsql maps <see cref="DateTime"/> to "timestamp with time zone", which rejects any value
/// whose <see cref="DateTimeKind"/> is not <see cref="DateTimeKind.Utc"/>. Values read back from
/// the database arrive as Unspecified, so they are re-tagged as UTC on materialization.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc
                ? v
                : v.Kind == DateTimeKind.Local
                    ? v.ToUniversalTime()
                    : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
