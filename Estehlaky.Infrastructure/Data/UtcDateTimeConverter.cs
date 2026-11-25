using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Estehlaky.Infrastructure.Data;

/// <summary>SQL Server returns DateTime with Kind=Unspecified. Everything we store is UTC, so mark it.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
