using System.Text.Json;

namespace Pra.Core.Persistence;

/// <summary>
/// JSON Lines file helpers: the interim persistent backing for the registry
/// and the decision/approval logs (plan, section 6: append-only evidence
/// storage). Each line is one self-contained record, so the log survives a
/// crash mid-write and can be tailed by the log pipeline. A production store
/// (immutable blob / Log Analytics) replaces this without changing callers:
/// they keep working against the registry and engines.
/// </summary>
public static class JsonLinesStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Appends one record as a single JSON line.</summary>
    public static void Append<T>(string path, T record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(record);

        var line = JsonSerializer.Serialize(record, Options);
        File.AppendAllText(path, line + Environment.NewLine);
    }

    /// <summary>Reads every line as a record, skipping blank lines.</summary>
    public static IReadOnlyList<T> ReadAll<T>(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            return [];

        return File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, Options)!)
            .ToArray();
    }

    /// <summary>Replaces the file with the given records (used for registry snapshots).</summary>
    public static void WriteAll<T>(string path, IEnumerable<T> records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(records);

        var lines = records.Select(r => JsonSerializer.Serialize(r, Options));
        File.WriteAllLines(path, lines);
    }
}
