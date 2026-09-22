namespace Pra.Core.Review;

/// <summary>Deterministic review checks that run without an LLM.</summary>
public interface IReviewCheck
{
    ReviewFindingKind Kind { get; }

    IReadOnlyList<ReviewFinding> Run(ReviewContext context);
}

/// <summary>Everything the Review agent inspects for one proposal draft.</summary>
public sealed record ReviewContext
{
    /// <summary>Compliance matrix rows: requirement ID → whether the draft covers it.</summary>
    public IReadOnlyDictionary<string, bool> RequirementCoverage { get; init; } =
        new Dictionary<string, bool>();

    /// <summary>Claims extracted from the draft, each expected to carry a [source-id] citation.</summary>
    public IReadOnlyList<string> Claims { get; init; } = [];

    /// <summary>Source IDs actually retrieved from the knowledge base for this run.</summary>
    public IReadOnlySet<string> RetrievedSourceIds { get; init; } = new HashSet<string>();
}

/// <summary>Result of one review pass over a draft.</summary>
public sealed record ReviewReport
{
    public required IReadOnlyList<ReviewFinding> Findings { get; init; }

    /// <summary>Coverage = covered requirements / total requirements; 1 when there are none.</summary>
    public double CoverageRatio { get; init; }

    public bool HasCriticalFindings => Findings.Any(f => f.Severity == ReviewSeverity.Critical);
}

/// <summary>
/// The Review agent (PRA-Core) as the critic in the proposal pipeline:
/// checks compliance-matrix coverage, unsupported claims, contractual
/// commitments, consistency and submission-format rules. Findings loop back
/// to the Writer; a maximum of 3 iterations applies before escalating to a
/// human (plan, section 11.9).
/// </summary>
public sealed class ProposalReviewPipeline
{
    /// <summary>Maximum writer/reviewer iterations before a human is pulled in.</summary>
    public const int MaxIterations = 3;

    private readonly IReadOnlyList<IReviewCheck> _checks;

    public ProposalReviewPipeline(IEnumerable<IReviewCheck>? checks = null)
    {
        _checks = (checks ?? DefaultChecks()).ToArray();
    }

    public ReviewReport Review(ReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = _checks.SelectMany(c => c.Run(context)).ToArray();

        var coverage = context.RequirementCoverage.Count == 0
            ? 1.0
            : context.RequirementCoverage.Values.Count(covered => covered)
                / (double)context.RequirementCoverage.Count;

        return new ReviewReport { Findings = findings, CoverageRatio = coverage };
    }

    private static IEnumerable<IReviewCheck> DefaultChecks()
    {
        yield return new CoverageCheck();
        yield return new UnsupportedClaimCheck();
    }

    private sealed class CoverageCheck : IReviewCheck
    {
        public ReviewFindingKind Kind => ReviewFindingKind.CoverageGap;

        public IReadOnlyList<ReviewFinding> Run(ReviewContext context) =>
            context.RequirementCoverage
                .Where(kv => !kv.Value)
                .Select(kv => new ReviewFinding
                {
                    Kind = ReviewFindingKind.CoverageGap,
                    Severity = ReviewSeverity.Critical,
                    Description = $"Requirement '{kv.Key}' has no response section and no explicit exclusion.",
                    Location = kv.Key,
                })
                .ToArray();
    }

    private sealed class UnsupportedClaimCheck : IReviewCheck
    {
        public ReviewFindingKind Kind => ReviewFindingKind.UnsupportedClaim;

        public IReadOnlyList<ReviewFinding> Run(ReviewContext context) =>
            ClaimCitationChecker.FindUnsupportedClaims(context.Claims, context.RetrievedSourceIds);
    }
}
