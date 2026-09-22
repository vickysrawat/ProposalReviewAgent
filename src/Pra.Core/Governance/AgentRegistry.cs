using System.Collections.Concurrent;

namespace Pra.Core.Governance;

/// <summary>
/// In-memory agent registry: single source of truth for owner, purpose, risk
/// tier, autonomy level, allowed tools and data classes. Runtime reads config
/// from it; nothing is hardcoded in agents. A persistent store backs this
/// interface in production.
/// </summary>
public sealed class AgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentRegistration> _agents =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(AgentRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (string.IsNullOrWhiteSpace(registration.AgentId))
            throw new ArgumentException("AgentId is required.", nameof(registration));

        if (!_agents.TryAdd(registration.AgentId, registration))
            throw new InvalidOperationException(
                $"Agent '{registration.AgentId}' is already registered.");
    }

    public void Update(AgentRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        _agents[registration.AgentId] = registration;
    }

    public AgentRegistration Get(string agentId) =>
        _agents.TryGetValue(agentId, out var registration)
            ? registration
            : throw new KeyNotFoundException($"Agent '{agentId}' is not registered.");

    public bool TryGet(string agentId, out AgentRegistration registration) =>
        _agents.TryGetValue(agentId, out registration!);

    public IReadOnlyCollection<AgentRegistration> All() =>
        _agents.Values.ToArray();
}
