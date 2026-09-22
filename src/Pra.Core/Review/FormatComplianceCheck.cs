namespace Pra.Core.Review;

/// <summary>
/// Checks mandatory submission rules from the RFP (plan, sections 11.4 and
/// 11.9): required sections present, page/word limits respected, submission
/// deadline not already passed. Misses are Critical — a mandatory-format miss
/// can disqualify the submission outright (plan, section 11.5).
/// </summary>
public sealed class FormatComplianceCheck : IReviewCheck
{
    public ReviewFindingKind Kind => ReviewFindingKind.FormatViolation;

    public IReadOnlyList<ReviewFinding> Run(ReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<ReviewFinding>();
        var rules = context.FormatRules;

        foreach (var required in rules.RequiredSections)
        {
            var present = context.Sections.Any(
                s => string.Equals(s.Name, required, StringComparison.OrdinalIgnoreCase));

            if (!present)
            {
                findings.Add(new ReviewFinding
                {
                    Kind = ReviewFindingKind.FormatViolation,
                    Severity = ReviewSeverity.Critical,
                    Description = $"Required section '{required}' is missing from the draft.",
                    Location = required,
                });
            }
        }

        if (rules.MaxPages is { } maxPages && context.PageCount > maxPages)
        {
            findings.Add(new ReviewFinding
            {
                Kind = ReviewFindingKind.FormatViolation,
                Severity = ReviewSeverity.Critical,
                Description = $"Draft is {context.PageCount} pages, exceeding the {maxPages}-page limit.",
            });
        }

        var wordCount = context.WordCount > 0
            ? context.WordCount
            : context.Sections.Sum(s => CountWords(s.Text));

        if (rules.MaxWords is { } maxWords && wordCount > maxWords)
        {
            findings.Add(new ReviewFinding
            {
                Kind = ReviewFindingKind.FormatViolation,
                Severity = ReviewSeverity.Critical,
                Description = $"Draft is {wordCount} words, exceeding the {maxWords}-word limit.",
            });
        }

        if (rules.SubmissionDeadline is { } deadline && deadline < DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            findings.Add(new ReviewFinding
            {
                Kind = ReviewFindingKind.FormatViolation,
                Severity = ReviewSeverity.Critical,
                Description = $"Submission deadline {deadline:yyyy-MM-dd} has already passed; the draft cannot be submitted on time.",
            });
        }

        return findings;
    }

    private static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
