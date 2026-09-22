using System.Text.RegularExpressions;

namespace Pra.Core.Review;

/// <summary>
/// Flags contractual-commitment language in the draft — SLAs, penalties and
/// fixed-price wording (plan, section 11.9). Every sentence containing
/// commitment language becomes one finding per commitment category, with the
/// sentence as the excerpt, so a reviewer can see exactly what the company
/// would be bound to. Commitments are never blocked silently: they are
/// escalated for legal/commercial sign-off.
/// </summary>
public sealed class ContractualCommitmentCheck : IReviewCheck
{
    public ReviewFindingKind Kind => ReviewFindingKind.ContractualCommitment;

    /// <summary>Binding-language patterns, grouped by the commitment they signal.</summary>
    private static readonly (string Category, Regex Pattern)[] Patterns =
    [
        ("service-level agreement", new Regex(@"\bSLA\b|service[- ]level", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("penalty or damages", new Regex(@"\b(penalt(y|ies)|liquidated damages|indemnif(y|ies|ication))\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("fixed-price commitment", new Regex(@"\bfixed[- ]price\b|\bfirm[- ]fixed\b|\blump[- ]sum\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("warranty or guarantee", new Regex(@"\b(warrant(y|ies)|guarantee(d|s)?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("unlimited liability", new Regex(@"\bunlimited liability\b|\ball losses\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
    ];

    /// <summary>Splits text into sentences on ., !, ?, and newlines.</summary>
    private static readonly Regex SentenceBoundary = new(
        @"(?<=[.!?])\s+|\r?\n", RegexOptions.Compiled);

    public IReadOnlyList<ReviewFinding> Run(ReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<ReviewFinding>();

        foreach (var section in context.Sections)
        {
            // One finding per (sentence, category); repeats of the same
            // category inside one sentence collapse into a single finding.
            foreach (var sentence in SentenceBoundary.Split(section.Text))
            {
                var trimmed = sentence.Trim();
                if (trimmed.Length == 0)
                    continue;

                foreach (var (category, pattern) in Patterns)
                {
                    if (!pattern.IsMatch(trimmed))
                        continue;

                    findings.Add(new ReviewFinding
                    {
                        Kind = ReviewFindingKind.ContractualCommitment,
                        Severity = ReviewSeverity.Warning,
                        Description = $"{category} language requires legal/commercial sign-off: \"{Truncate(trimmed)}\"",
                        Location = section.Name,
                    });
                }
            }
        }

        return findings;
    }

    private static string Truncate(string text, int max = 200) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max), "…");
}
