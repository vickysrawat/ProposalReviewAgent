using System.Text.RegularExpressions;

namespace Pra.Core.Review;

/// <summary>
/// Flags contractual-commitment language in the draft — SLAs, penalties and
/// fixed-price wording (plan, section 11.9). Commitments are never blocked
/// silently: every one is escalated as a finding so legal/commercial can
/// confirm the company is willing to be bound by it.
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

    public IReadOnlyList<ReviewFinding> Run(ReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<ReviewFinding>();

        foreach (var section in context.Sections)
        {
            foreach (var (category, pattern) in Patterns)
            {
                if (!pattern.IsMatch(section.Text))
                    continue;

                findings.Add(new ReviewFinding
                {
                    Kind = ReviewFindingKind.ContractualCommitment,
                    Severity = ReviewSeverity.Warning,
                    Description = $"Section contains {category} language; route to legal/commercial for sign-off before submission.",
                    Location = section.Name,
                });
            }
        }

        return findings;
    }
}
