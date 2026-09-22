using Pra.Core.Review;
using Xunit;

namespace Pra.Core.Tests.Review;

public class ClaimCitationCheckerTests
{
    [Theory]
    [InlineData("Certified [kb-1]", "kb-1")]
    [InlineData("See case study [case-contoso-retail] for details", "case-contoso-retail")]
    public void Extracts_citation_marker(string claim, string expected)
    {
        Assert.Equal(expected, ClaimCitationChecker.ExtractCitationMarker(claim));
    }

    [Theory]
    [InlineData("No citation here")]
    [InlineData("Empty [] marker")]
    public void Returns_null_without_marker(string claim)
    {
        Assert.Null(ClaimCitationChecker.ExtractCitationMarker(claim));
    }

    [Fact]
    public void Last_bracket_span_wins_over_earlier_prose_brackets()
    {
        var claim = "See [Appendix B] for our ISO 27001 certification [kb-cert-iso27001]";

        Assert.Equal("kb-cert-iso27001", ClaimCitationChecker.ExtractCitationMarker(claim));
    }

    [Fact]
    public void Claim_with_prose_bracket_before_real_citation_is_not_flagged()
    {
        var findings = ClaimCitationChecker.FindUnsupportedClaims(
            ["See [Appendix B] for details [kb-case-study]"],
            new HashSet<string> { "kb-case-study" });

        Assert.Empty(findings);
    }

    [Fact]
    public void Unterminated_bracket_is_not_a_marker()
    {
        Assert.Null(ClaimCitationChecker.ExtractCitationMarker("Dangling [bracket"));
    }
}
