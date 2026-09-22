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
    /// carry a citation marker of the form [source-id].
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

    /// <summary>Extracts the first [source-id] citation marker from a claim, if present.</summary>
    public static string? ExtractCitationMarker(string claim)
    {
        var open = claim.IndexOf('[', StringComparison.Ordinal);
        var close = open >= 0 ? claim.IndexOf(']', open + 1) : -1;

        if (open < 0 || close <= open + 1)
            return null;

        return claim.Substring(open + 1, close - open - 1).Trim();
    }
}
