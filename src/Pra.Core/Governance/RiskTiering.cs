namespace Pra.Core.Governance;

/// <summary>
/// The five assessment factors from the risk-tiering rubric (plan, section 4).
/// </summary>
public sealed record RiskFactorAssessment
{
    public required RiskTier DataTouched { get; init; }
    public required RiskTier Actions { get; init; }
    public required RiskTier Autonomy { get; init; }
    public required RiskTier AudienceImpact { get; init; }
    public required RiskTier RegulatoryExposure { get; init; }

    /// <summary>The agent takes the highest tier any single factor puts it in.</summary>
    public RiskTier OverallTier =>
        new[] { DataTouched, Actions, Autonomy, AudienceImpact, RegulatoryExposure }.Max();
}

/// <summary>
/// Runtime controls implied by a tier (plan, section 4, control table).
/// The registry writes the tier and runtime config is derived from it;
/// nothing is hardcoded in agents.
/// </summary>
public sealed record RuntimeControls
{
    public required bool HumanApprovalOnWrites { get; init; }
    public required bool HumanApprovalOnExternalActions { get; init; }
    public required bool HumanApprovalOnIrreversibleActions { get; init; }
    public required bool RequiresBoardApproval { get; init; }
    public required bool RedTeamBeforeRelease { get; init; }
    public required TimeSpan TraceRetention { get; init; }
    public required TimeSpan AccessReviewCadence { get; init; }

    public static RuntimeControls ForTier(RiskTier tier) => tier switch
    {
        RiskTier.Low => new RuntimeControls
        {
            HumanApprovalOnWrites = false,
            HumanApprovalOnExternalActions = false,
            HumanApprovalOnIrreversibleActions = false,
            RequiresBoardApproval = false,
            RedTeamBeforeRelease = false,
            TraceRetention = TimeSpan.FromDays(90),
            AccessReviewCadence = TimeSpan.FromDays(365),
        },
        RiskTier.Medium => new RuntimeControls
        {
            HumanApprovalOnWrites = true,
            HumanApprovalOnExternalActions = true,
            HumanApprovalOnIrreversibleActions = true,
            RequiresBoardApproval = false,
            RedTeamBeforeRelease = true,
            TraceRetention = TimeSpan.FromDays(365),
            AccessReviewCadence = TimeSpan.FromDays(182),
        },
        RiskTier.High => new RuntimeControls
        {
            HumanApprovalOnWrites = true,
            HumanApprovalOnExternalActions = true,
            HumanApprovalOnIrreversibleActions = true,
            RequiresBoardApproval = true,
            RedTeamBeforeRelease = true,
            TraceRetention = TimeSpan.FromDays(365),
            AccessReviewCadence = TimeSpan.FromDays(91),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown risk tier."),
    };
}
