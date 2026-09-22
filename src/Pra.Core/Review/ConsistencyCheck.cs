namespace Pra.Core.Review;

/// <summary>
/// Checks that the plan, the estimate and the narrative agree with each other
/// (plan, section 11.9). Each consistency figure is a keyed set of values from
/// different artifacts (e.g. "total-effort-hours": plan = 1200, estimate = 1500);
/// when the values diverge beyond the tolerance, the discrepancy is flagged
/// instead of submitted. Tolerance defaults to 1% to absorb rounding from
/// upstream artifacts; set it to 0 for exact matching.
/// </summary>
public sealed class ConsistencyCheck : IReviewCheck
{
    private readonly double _relativeTolerance;

    public ConsistencyCheck(double relativeTolerance = 0.01)
    {
        if (relativeTolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(relativeTolerance), relativeTolerance, "Tolerance cannot be negative.");

        _relativeTolerance = relativeTolerance;
    }

    public ReviewFindingKind Kind => ReviewFindingKind.Inconsistency;

    public IReadOnlyList<ReviewFinding> Run(ReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<ReviewFinding>();

        foreach (var figure in context.ConsistencyFigures)
        {
            var values = figure.Values;
            if (values.Count < 2)
                continue;

            var max = values.Values.Max();
            var min = values.Values.Min();

            if (Agrees(max, min))
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

    private bool Agrees(double max, double min)
    {
        var spread = max - min;
        var scale = Math.Max(Math.Abs(max), Math.Abs(min));

        // Zero vs zero agrees; zero vs anything else agrees only within an
        // absolute band of the tolerance (there is no scale to relate to).
        return scale == 0 ? spread == 0 : spread <= _relativeTolerance * scale;
    }
}
