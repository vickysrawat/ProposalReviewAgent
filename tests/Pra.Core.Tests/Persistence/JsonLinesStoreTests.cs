using Pra.Core.Persistence;
using Xunit;

namespace Pra.Core.Tests.Persistence;

public class JsonLinesStoreTests
{
    private sealed record Sample(int Id, string Name);

    [Fact]
    public void Append_then_readall_round_trips_records()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pra-jsonl-{Guid.NewGuid():N}.jsonl");
        try
        {
            JsonLinesStore.Append(path, new Sample(1, "one"));
            JsonLinesStore.Append(path, new Sample(2, "two"));

            var records = JsonLinesStore.ReadAll<Sample>(path);

            Assert.Equal(2, records.Count);
            Assert.Equal(1, records[0].Id);
            Assert.Equal("two", records[1].Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Readall_on_missing_file_returns_empty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pra-jsonl-{Guid.NewGuid():N}.jsonl");

        Assert.Empty(JsonLinesStore.ReadAll<Sample>(path));
    }

    [Fact]
    public void Writeall_replaces_the_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pra-jsonl-{Guid.NewGuid():N}.jsonl");
        try
        {
            JsonLinesStore.Append(path, new Sample(9, "old"));
            JsonLinesStore.WriteAll(path, [new Sample(1, "a"), new Sample(2, "b")]);

            var records = JsonLinesStore.ReadAll<Sample>(path);

            Assert.Equal(2, records.Count);
            Assert.DoesNotContain(records, r => r.Name == "old");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
