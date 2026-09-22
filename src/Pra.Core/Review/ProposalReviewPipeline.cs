namespace Pra.Core.Review;

/// <summary>Deterministic review checks that run without an LLM.</summary>
public interface IReviewCheck
{
    ReviewFindingKind Kind { get; }

    IReadOnlyList<ReviewFinding> Run(ReviewContext context);
}

/// <summary>A named section of the draft, with its full text.</summary>
public sealed record DraftSection
{
    public required string Name { get; init; }
    public required string Text { get; init; }
}

/// <summary>Submission-format rules from the RFP (plan, section 11.4: format, page limits, deadlines).</summary>
public sealed record SubmissionFormatRules
{
    /// <summary>Sections the client requires, by name.</summary>
    public IReadOnlyList<string> RequiredSections { get; init; } = [];

    /// <summary>Maximum page count, when the RFP sets one.</summary>
    public int? MaxPages { get; init; }

    /// <summary>Maximum word count, when the RFP sets one.</summary>
    public int? MaxWords { get; init; }
}

/// <summary>
/// A figure that must agree across artifacts (plan, estimate, narrative) —
/// e.g. total effort hours, duration in weeks, team size.
/// </summary>
public sealed record ConsistencyFigure
{
    public required string Name { get; init; }

    /// <summary>Artifact name → value, e.g. "plan" → 1200, "estimate" → 1500.</summary>
    public IReadOnlyDictionary<string, double> Values { get; init; } =
        new Dictionary<string, double>();
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

    /// <summary>The draft's named sections and their text.</summary>
    public IReadOnlyList<DraftSection> Sections { get; init; } = [];

    /// <summary>Format rules the submission must satisfy.</summary>
    public SubmissionFormatRules FormatRules { get; init; } = new();

    /// <summary>Length of the current draft.</summary>
    public int PageCount { get; init; }
    public int WordCount { get; init; }

    /// <summary>Figures that must agree across the plan, estimate and narrative.</summary>
    public IReadOnlyList<ConsistencyFigure> ConsistencyFigures { get; init; } = [];
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
        yield return new ContractualCommitmentCheck();
        yield return new FormatComplianceCheck();
        yield return new ConsistencyCheck();
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
