using Pra.Core.Review;
using Xunit;

namespace Pra.Core.Tests.Review;

public class ProposalReviewPipelineTests
{
    [Fact]
    public void Fully_covered_cited_draft_produces_no_findings()
    {
        var pipeline = new ProposalReviewPipeline();
        var context = new ReviewContext
        {
            RequirementCoverage = new Dictionary<string, bool>
            {
                ["REQ-1"] = true,
                ["REQ-2"] = true,
            },
            Claims = ["We are ISO 27001 certified [kb-cert-iso27001]"],
            RetrievedSourceIds = new HashSet<string> { "kb-cert-iso27001" },
        };

        var report = pipeline.Review(context);

        Assert.Empty(report.Findings);
        Assert.Equal(1.0, report.CoverageRatio);
        Assert.False(report.HasCriticalFindings);
    }

    [Fact]
    public void Uncovered_requirement_is_a_critical_coverage_gap()
    {
        var pipeline = new ProposalReviewPipeline();
        var context = new ReviewContext
        {
            RequirementCoverage = new Dictionary<string, bool>
            {
                ["REQ-1"] = true,
                ["REQ-7"] = false,
            },
        };

        var report = pipeline.Review(context);

        var finding = Assert.Single(report.Findings);
        Assert.Equal(ReviewFindingKind.CoverageGap, finding.Kind);
        Assert.Equal(ReviewSeverity.Critical, finding.Severity);
        Assert.Equal(0.5, report.CoverageRatio);
        Assert.True(report.HasCriticalFindings);
    }

    [Fact]
    public void Claim_without_citation_is_flagged()
    {
        var pipeline = new ProposalReviewPipeline();
        var context = new ReviewContext
        {
            Claims = ["We delivered a similar platform for a Fortune 500 retailer"],
        };

        var report = pipeline.Review(context);

        var finding = Assert.Single(report.Findings);
        Assert.Equal(ReviewFindingKind.UnsupportedClaim, finding.Kind);
    }

    [Fact]
    public void Claim_citing_unretrieved_source_is_flagged()
    {
        var pipeline = new ProposalReviewPipeline();
        var context = new ReviewContext
        {
            Claims = ["Certified per [kb-cert-not-loaded]"],
            RetrievedSourceIds = new HashSet<string> { "kb-cert-iso27001" },
        };

        var report = pipeline.Review(context);

        var finding = Assert.Single(report.Findings);
        Assert.Equal(ReviewFindingKind.UnsupportedClaim, finding.Kind);
        Assert.Equal("kb-cert-not-loaded", finding.Citation);
    }

    [Fact]
    public void Writer_loop_is_capped_at_three_iterations()
    {
        Assert.Equal(3, ProposalReviewPipeline.MaxIterations);
    }
}
