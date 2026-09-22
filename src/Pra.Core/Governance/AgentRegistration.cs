namespace Pra.Core.Governance;

/// <summary>RACI ownership: business owns outcomes, tech owns behavior, risk signs off the tier.</summary>
public sealed record AgentOwners
{
    public required string Business { get; init; }
    public required string Technical { get; init; }
    public required string Risk { get; init; }
}

/// <summary>A model the agent is approved to use, with its approved version.</summary>
public sealed record ApprovedModel
{
    public required string Provider { get; init; }
    public required string Deployment { get; init; }
    public string? ApprovedVersion { get; init; }
}

/// <summary>A tool the agent may call, with its scope and approval requirements.</summary>
public sealed record ToolGrant
{
    public required string Name { get; init; }

    /// <summary>"read" or "write".</summary>
    public required string Scope { get; init; }

    /// <summary>Whether calls to this tool always require human approval.</summary>
    public bool RequiresApproval { get; init; }

    /// <summary>Whether the tool performs external communication.</summary>
    public bool External { get; init; }
}

/// <summary>Blast-radius limits enforced per run (plan, section 5).</summary>
public sealed record AgentLimits
{
    public int MaxStepsPerRun { get; init; } = 25;
    public double MaxCostUsdPerRun { get; init; } = 2.0;
    public int MaxToolCallsPerMinute { get; init; } = 30;
}

/// <summary>Lifecycle status from the agent release lifecycle (plan, section 2).</summary>
public enum AgentStatus
{
    Proposed,
    Registered,
    Tiered,
    InTest,
    Approved,
    Production,
    Suspended,
    Retired,
}

/// <summary>
/// One registry record per agent. Mirrors the registry schema in section 3 of
/// the governance plan. The gateway, guardrails and dashboards all read from
/// this record.
/// </summary>
public sealed record AgentRegistration
{
    public required string AgentId { get; init; }
    public required string Version { get; init; }
    public required string DisplayName { get; init; }
    public required string Purpose { get; init; }
    public required AgentOwners Owners { get; init; }
    public required RiskTier RiskTier { get; init; }
    public required AutonomyLevel AutonomyLevel { get; init; }
    public IReadOnlyList<ApprovedModel> Models { get; init; } = [];
    public IReadOnlyList<ToolGrant> Tools { get; init; } = [];
    public IReadOnlyList<string> AllowedDataLabels { get; init; } = [];
    public IReadOnlyList<string> BlockedDataLabels { get; init; } = [];
    public IReadOnlyList<string> PolicyIds { get; init; } = [];
    public AgentLimits Limits { get; init; } = new();
    public IReadOnlyList<string> RegulatoryTags { get; init; } = [];
    public AgentStatus Status { get; init; } = AgentStatus.Proposed;
    public DateOnly LastReviewed { get; init; }

    /// <summary>Runtime controls derived from the tier — never overridden per agent.</summary>
    public RuntimeControls Controls => RuntimeControls.ForTier(RiskTier);
}
