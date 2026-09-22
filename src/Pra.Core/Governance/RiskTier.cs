namespace Pra.Core.Governance;

/// <summary>
/// Risk tier assigned by the rubric in section 4 of the governance plan.
/// An agent takes the highest tier any single factor puts it in.
/// </summary>
public enum RiskTier
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/// <summary>
/// Autonomy levels from the governance plan (section 2).
/// L1 suggest only, L2 act with human approval, L3 act autonomously within
/// limits, L4 autonomous with irreversible actions (rare, board approval).
/// </summary>
public enum AutonomyLevel
{
    L1 = 1,
    L2 = 2,
    L3 = 3,
    L4 = 4,
}
