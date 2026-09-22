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
}
