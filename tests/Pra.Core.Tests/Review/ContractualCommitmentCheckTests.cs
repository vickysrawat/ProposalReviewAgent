using Pra.Core.Review;
using Xunit;

namespace Pra.Core.Tests.Review;

public class ContractualCommitmentCheckTests
{
    private readonly ContractualCommitmentCheck _check = new();

    [Fact]
    public void Sla_language_is_flagged_for_legal_review()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection { Name = "Solution", Text = "We guarantee 99.9% uptime under the SLA." },
            ],
        };

        var findings = _check.Run(context);

        Assert.Contains(findings, f =>
            f.Kind == ReviewFindingKind.ContractualCommitment &&
            f.Location == "Solution" &&
            f.Description.Contains("service-level", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Fixed_price_language_is_flagged()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection { Name = "Commercials", Text = "We offer this on a fixed-price basis." },
            ],
        };

        var findings = _check.Run(context);

        Assert.Contains(findings, f =>
            f.Kind == ReviewFindingKind.ContractualCommitment &&
            f.Description.Contains("fixed-price", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Penalty_and_indemnity_language_is_flagged()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection { Name = "Terms", Text = "We accept penalties for late delivery and will indemnify the client." },
            ],
        };

        var findings = _check.Run(context);

        Assert.Contains(findings, f => f.Description.Contains("penalty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Clean_section_produces_no_findings()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection { Name = "Approach", Text = "We deliver iteratively with weekly demos." },
            ],
        };

        Assert.Empty(_check.Run(context));
    }

    [Fact]
    public void Findings_include_the_offending_sentence_and_section()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection
                {
                    Name = "Solution",
                    Text = "We deliver weekly. We commit to a 99.9% SLA. Our team is senior.",
                },
            ],
        };

        var finding = Assert.Single(_check.Run(context));
        Assert.Equal("Solution", finding.Location);
        Assert.Contains("99.9% SLA", finding.Description);
        Assert.DoesNotContain("deliver weekly", finding.Description);
    }

    [Fact]
    public void One_sentence_can_carry_multiple_commitment_categories()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection
                {
                    Name = "Solution",
                    Text = "We guarantee 99.9% uptime under the SLA.",
                },
            ],
        };

        var findings = _check.Run(context);

        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal("Solution", f.Location));
    }

    [Fact]
    public void Each_committing_sentence_is_flagged_separately()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection
                {
                    Name = "Terms",
                    Text = "We guarantee response times. We accept penalties for delay.",
                },
            ],
        };

        var findings = _check.Run(context);

        Assert.Equal(2, findings.Count);
        Assert.Contains(findings, f => f.Description.Contains("guarantee"));
        Assert.Contains(findings, f => f.Description.Contains("penalt"));
    }

    [Fact]
    public void Findings_are_warnings_not_critical()
    {
        var context = new ReviewContext
        {
            Sections =
            [
                new DraftSection { Name = "Terms", Text = "Unlimited liability applies to all losses." },
            ],
        };

        var findings = _check.Run(context);

        Assert.NotEmpty(findings);
        Assert.All(findings, f => Assert.Equal(ReviewSeverity.Warning, f.Severity));
    }
}
