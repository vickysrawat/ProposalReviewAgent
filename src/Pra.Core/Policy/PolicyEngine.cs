using Pra.Core.Governance;

namespace Pra.Core.Policy;

/// <summary>Pre-action filter verdict: allow, block, or escalate to HITL.</summary>
public enum PolicyDecisionKind
{
    Allow,
    Block,
    Escalate,
}

/// <summary>
/// A logged policy decision for one tool call. Every decision event is also an
/// audit record (plan, section 8: Security ∩ Compliance overlap).
/// </summary>
public sealed record PolicyDecision
{
    public required PolicyDecisionKind Kind { get; init; }
    public required string AgentId { get; init; }
    public required string ToolName { get; init; }
    public required string Reason { get; init; }
    public string? PolicyId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static PolicyDecision Allow(string agentId, string toolName, string reason) =>
        new() { Kind = PolicyDecisionKind.Allow, AgentId = agentId, ToolName = toolName, Reason = reason };

    public static PolicyDecision Block(string agentId, string toolName, string reason, string? policyId = null) =>
        new() { Kind = PolicyDecisionKind.Block, AgentId = agentId, ToolName = toolName, Reason = reason, PolicyId = policyId };

    public static PolicyDecision Escalate(string agentId, string toolName, string reason, string? policyId = null) =>
        new() { Kind = PolicyDecisionKind.Escalate, AgentId = agentId, ToolName = toolName, Reason = reason, PolicyId = policyId };
}

/// <summary>A single proposed tool call, evaluated before execution.</summary>
public sealed record ToolCallRequest
{
    public required string AgentId { get; init; }
    public required string ToolName { get; init; }

    /// <summary>Sensitivity labels of the data the call reads or writes.</summary>
    public IReadOnlyList<string> DataLabels { get; init; } = [];
}

/// <summary>
/// Pre-action filter: evaluates each tool call against the agent's registry
/// record and tier-derived controls before it reaches the tool gateway
/// (plan, section 5). Every decision is appended to the decision log so a
/// blocked or escalated call is also an audit record.
/// </summary>
public sealed class PolicyEngine
{
    private readonly AgentRegistry _registry;
    private readonly List<PolicyDecision> _decisionLog = [];
    private readonly object _logLock = new();

    public PolicyEngine(AgentRegistry registry)
    {
        _registry = registry;
    }

    public PolicyDecision Evaluate(ToolCallRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var agent = _registry.Get(request.AgentId);
        var decision = EvaluateCore(agent, request);

        lock (_logLock)
        {
            _decisionLog.Add(decision);
        }

        return decision;
    }

    /// <summary>All decisions logged so far, in order.</summary>
    public IReadOnlyList<PolicyDecision> DecisionLog
    {
        get
        {
            lock (_logLock)
            {
                return _decisionLog.ToArray();
            }
        }
    }

    private static PolicyDecision EvaluateCore(AgentRegistration agent, ToolCallRequest request)
    {
        if (agent.Status is AgentStatus.Suspended or AgentStatus.Retired)
            return PolicyDecision.Block(agent.AgentId, request.ToolName,
                $"Agent is {agent.Status.ToString().ToLowerInvariant()} and cannot invoke tools.");

        var grant = agent.Tools.FirstOrDefault(
            t => string.Equals(t.Name, request.ToolName, StringComparison.OrdinalIgnoreCase));

        if (grant is null)
            return PolicyDecision.Block(agent.AgentId, request.ToolName,
                "Tool is not on the agent's allow-list.", "POL-TOOL-ALLOWLIST");

        var blockedLabel = request.DataLabels.FirstOrDefault(
            label => agent.BlockedDataLabels.Contains(label, StringComparer.OrdinalIgnoreCase));
        if (blockedLabel is not null)
            return PolicyDecision.Block(agent.AgentId, request.ToolName,
                $"Data label '{blockedLabel}' is blocked for this agent.");

        if (agent.AllowedDataLabels.Count > 0)
        {
            var unknownLabel = request.DataLabels.FirstOrDefault(
                label => !agent.AllowedDataLabels.Contains(label, StringComparer.OrdinalIgnoreCase));
            if (unknownLabel is not null)
                return PolicyDecision.Escalate(agent.AgentId, request.ToolName,
                    $"Data label '{unknownLabel}' is outside the agent's allowed labels.");
        }

        if (grant.RequiresApproval)
            return PolicyDecision.Escalate(agent.AgentId, request.ToolName,
                "Tool requires human approval.", "POL-HITL-EXT-COMMS");

        var controls = agent.Controls;
        if (grant.External && controls.HumanApprovalOnExternalActions)
            return PolicyDecision.Escalate(agent.AgentId, request.ToolName,
                $"External action requires human approval at {agent.RiskTier} tier.", "POL-HITL-EXT-COMMS");

        if (grant.Scope == "write" && controls.HumanApprovalOnWrites)
            return PolicyDecision.Escalate(agent.AgentId, request.ToolName,
                $"Write action requires human approval at {agent.RiskTier} tier.");

        return PolicyDecision.Allow(agent.AgentId, request.ToolName,
            "Tool is allow-listed and within tier controls.");
    }
}
