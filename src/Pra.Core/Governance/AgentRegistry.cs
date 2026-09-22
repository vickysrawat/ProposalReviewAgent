using System.Collections.Concurrent;
using Pra.Core.Persistence;

namespace Pra.Core.Governance;

/// <summary>
/// In-memory agent registry: single source of truth for owner, purpose, risk
/// tier, autonomy level, allowed tools and data classes. Runtime reads config
/// from it; nothing is hardcoded in agents. When constructed with a path,
/// every mutation is persisted as a JSONL snapshot so the registry survives
/// restarts; a production store backs the same surface via the file layout in
/// <see cref="JsonLinesStore"/>.
/// </summary>
public sealed class AgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentRegistration> _agents =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly string? _snapshotPath;
    private readonly object _persistLock = new();

    public AgentRegistry(string? snapshotPath = null)
    {
        _snapshotPath = snapshotPath;

        if (_snapshotPath is not null && File.Exists(_snapshotPath))
        {
            foreach (var registration in JsonLinesStore.ReadAll<AgentRegistration>(_snapshotPath))
            {
                _agents[registration.AgentId] = registration;
            }
        }
    }

    public void Register(AgentRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (string.IsNullOrWhiteSpace(registration.AgentId))
            throw new ArgumentException("AgentId is required.", nameof(registration));

        if (!_agents.TryAdd(registration.AgentId, registration))
            throw new InvalidOperationException(
                $"Agent '{registration.AgentId}' is already registered.");

        PersistSnapshot();
    }

    public void Update(AgentRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        _agents[registration.AgentId] = registration;
        PersistSnapshot();
    }

    public AgentRegistration Get(string agentId) =>
        _agents.TryGetValue(agentId, out var registration)
            ? registration
            : throw new KeyNotFoundException($"Agent '{agentId}' is not registered.");

    public bool TryGet(string agentId, out AgentRegistration registration) =>
        _agents.TryGetValue(agentId, out registration!);

    public IReadOnlyCollection<AgentRegistration> All() =>
        _agents.Values.ToArray();

    private void PersistSnapshot()
    {
        if (_snapshotPath is null)
            return;

        lock (_persistLock)
        {
            JsonLinesStore.WriteAll(_snapshotPath, _agents.Values);
        }
    }
}
