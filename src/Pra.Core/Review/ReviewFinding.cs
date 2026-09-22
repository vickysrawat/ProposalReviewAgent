namespace Pra.Core.Review;

/// <summary>Categories the Review agent checks (plan, section 11.9).</summary>
public enum ReviewFindingKind
{
    /// <summary>A compliance-matrix row with no matching response section or explicit exclusion.</summary>
    CoverageGap,

    /// <summary>A capability, certification or case-study claim with no knowledge-base citation.</summary>
    UnsupportedClaim,

    /// <summary>Contractual commitment language: SLAs, penalties, fixed-price wording.</summary>
    ContractualCommitment,

    /// <summary>Plan, estimate and narrative disagree with each other.</summary>
    Inconsistency,

    /// <summary>Submission-format rule violated (page limits, required sections, deadlines).</summary>
    FormatViolation,
}

public enum ReviewSeverity
{
    Info,
    Warning,
    Critical,
}

/// <summary>One finding produced by the Review agent. Findings loop back to the Writer.</summary>
public sealed record ReviewFinding
{
    public required ReviewFindingKind Kind { get; init; }
    public required ReviewSeverity Severity { get; init; }
    public required string Description { get; init; }

    /// <summary>Location in the draft (section or page) the finding refers to.</summary>
    public string? Location { get; init; }

    /// <summary>Knowledge-base citation supporting the finding, when applicable.</summary>
    public string? Citation { get; init; }
}
