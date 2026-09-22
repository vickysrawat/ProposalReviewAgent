namespace Pra.Core.Review;

/// <summary>
/// Anti-hallucination check from the PRA-Core framework (plan, section 11.6):
/// every capability claim, certification or case study must cite a
/// knowledge-base source. Unsupported claims are flagged, not written.
/// </summary>
public static class ClaimCitationChecker
{
    /// <summary>
    /// Flags claims whose citation marker does not resolve against the set of
    /// citations retrieved from the knowledge base. Claims are expected to
    /// carry a citation marker of the form [source-id]; when a claim carries
    /// several bracketed spans, the last one is the citation and earlier ones
    /// are treated as ordinary text (e.g. cross-references like [Appendix B]).
    /// </summary>
    public static IReadOnlyList<ReviewFinding> FindUnsupportedClaims(
        IEnumerable<string> claims,
        IReadOnlySet<string> retrievedSourceIds)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentNullException.ThrowIfNull(retrievedSourceIds);

        var findings = new List<ReviewFinding>();

        foreach (var claim in claims)
        {
            var marker = ExtractCitationMarker(claim);

            if (marker is null)
            {
                findings.Add(new ReviewFinding
                {
                    Kind = ReviewFindingKind.UnsupportedClaim,
                    Severity = ReviewSeverity.Critical,
                    Description = "Claim carries no citation marker and must not be written.",
                    Location = claim,
                });
            }
            else if (!retrievedSourceIds.Contains(marker))
            {
                findings.Add(new ReviewFinding
                {
                    Kind = ReviewFindingKind.UnsupportedClaim,
                    Severity = ReviewSeverity.Critical,
                    Description = $"Claim cites source '{marker}' which was not retrieved from the knowledge base.",
                    Location = claim,
                    Citation = marker,
                });
            }
        }

        return findings;
    }

    /// <summary>
    /// Extracts the citation marker from a claim: the last non-empty
    /// [source-id] span, so prose brackets like [Appendix B] before the real
    /// citation do not mask it. Returns null when there is no usable marker.
    /// </summary>
    public static string? ExtractCitationMarker(string claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        string? marker = null;
        var index = 0;

        while (index < claim.Length)
        {
            var open = claim.IndexOf('[', index);
            if (open < 0)
                break;

            var close = claim.IndexOf(']', open + 1);
            if (close < 0)
                break;

            var candidate = claim.Substring(open + 1, close - open - 1).Trim();
            if (candidate.Length > 0)
                marker = candidate;

            index = close + 1;
        }

        return marker;
    }
}
