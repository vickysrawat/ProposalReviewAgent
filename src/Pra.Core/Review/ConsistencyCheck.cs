namespace Pra.Core.Review;

/// <summary>
/// Checks that the plan, the estimate and the narrative agree with each other
/// (plan, section 11.9). Each consistency figure is a keyed set of values from
/// different artifacts (e.g. "total-effort-hours": plan = 1200, estimate = 1500);
/// when the values disagree, the discrepancy is flagged instead of submitted.
/// </summary>
public sealed class ConsistencyCheck : IReviewCheck
{
    public ReviewFindingKind Kind => ReviewFindingKind.Inconsistency;

    /// <summary>Relative tolerance before two numeric values count as disagreeing.</summary>
    private const double Tolerance = 0.0;

    public IReadOnlyList<ReviewFinding> Run(ReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<ReviewFinding>();

        foreach (var figure in context.ConsistencyFigures)
        {
            var values = figure.Values;
            if (values.Count < 2)
                continue;

            var distinct = values.Values.Distinct().ToArray();
            if (distinct.Length < 2)
                continue;

            var max = values.Values.Max();
            var min = values.Values.Min();

            if (Math.Abs(max - min) <= Tolerance * Math.Max(Math.Abs(max), Math.Abs(min)))
                continue;

            var detail = string.Join(", ", values.Select(kv => $"{kv.Key} = {kv.Value}"));
            findings.Add(new ReviewFinding
            {
                Kind = ReviewFindingKind.Inconsistency,
                Severity = ReviewSeverity.Warning,
                Description = $"Figure '{figure.Name}' disagrees across artifacts: {detail}. Reconcile plan, estimate and narrative before submission.",
                Location = figure.Name,
            });
        }

        return findings;
    }
}
