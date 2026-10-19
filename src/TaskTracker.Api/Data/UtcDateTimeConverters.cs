using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskTracker.Api.Data;

// SQLite stores dates as plain text without a time zone, so EF Core reads them back
// with Kind = Unspecified and the JSON loses the trailing "Z". These converters treat
// every stored date as UTC, so the API always returns unambiguous UTC timestamps.
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    v => v == null ? v : (v.Value.Kind == DateTimeKind.Local
        ? v.Value.ToUniversalTime()
        : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
    v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));
