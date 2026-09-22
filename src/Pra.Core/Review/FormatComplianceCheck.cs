namespace Pra.Core.Review;

/// <summary>
/// Checks submission-format rules (plan, section 11.9): required sections
/// present, page limits respected. Missing mandatory format items are
/// Critical — a format miss can disqualify the submission outright
/// (plan, section 11.5: "Missed mandatory requirement → disqualification").
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

        if (rules.MaxWords is { } maxWords && context.WordCount > maxWords)
        {
            findings.Add(new ReviewFinding
            {
                Kind = ReviewFindingKind.FormatViolation,
                Severity = ReviewSeverity.Critical,
                Description = $"Draft is {context.WordCount} words, exceeding the {maxWords}-word limit.",
            });
        }

        return findings;
    }
}
