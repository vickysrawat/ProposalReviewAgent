using Pra.Core.Governance;

namespace Pra.Core.Llm;

/// <summary>
/// Routes each completion to an approved model for the calling agent: cheaper
/// deployments for parsing, stronger ones for solutioning and writing. Only
/// deployments present in the agent's registry record may be used.
/// </summary>
public sealed class ModelRouter
{
    private readonly AgentRegistry _registry;
    private readonly IReadOnlyDictionary<ModelTaskClass, string> _taskDefaults;

    public ModelRouter(AgentRegistry registry, IReadOnlyDictionary<ModelTaskClass, string>? taskDefaults = null)
    {
        _registry = registry;
        _taskDefaults = taskDefaults ?? new Dictionary<ModelTaskClass, string>
        {
            [ModelTaskClass.Parsing] = "cheap",
            [ModelTaskClass.Extraction] = "cheap",
            [ModelTaskClass.Solutioning] = "strong",
            [ModelTaskClass.Writing] = "strong",
            [ModelTaskClass.Review] = "strong",
        };
    }

    /// <summary>
    /// Picks the deployment for a task. Throws when the agent has no approved
    /// model for the task class — agents never call unregistered models.
    /// </summary>
    public ApprovedModel Resolve(string agentId, ModelTaskClass taskClass)
    {
        var agent = _registry.Get(agentId);

        if (agent.Models.Count == 0)
            throw new InvalidOperationException($"Agent '{agentId}' has no approved models in the registry.");

        if (!_taskDefaults.TryGetValue(taskClass, out var tier))
            throw new ArgumentOutOfRangeException(nameof(taskClass), taskClass, "Unknown task class.");

        // Convention: deployment names carry their cost tier ("cheap"/"strong").
        var match = agent.Models.FirstOrDefault(
            m => m.Deployment.Contains(tier, StringComparison.OrdinalIgnoreCase));

        return match ?? throw new InvalidOperationException(
            $"Agent '{agentId}' has no approved '{tier}' deployment for task class {taskClass}. " +
            "Register a matching deployment; the router never falls back to an arbitrary model.");
    }
}
