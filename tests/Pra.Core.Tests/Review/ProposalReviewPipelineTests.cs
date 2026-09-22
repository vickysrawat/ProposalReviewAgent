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

    [Fact]
    public void Full_draft_runs_all_default_checks()
    {
        var pipeline = new ProposalReviewPipeline();
        var context = new ReviewContext
        {
            RequirementCoverage = new Dictionary<string, bool>
            {
                ["REQ-1"] = true,
                ["REQ-2"] = false,
            },
            Claims = ["We are ISO 27001 certified [kb-cert-iso27001]"],
            RetrievedSourceIds = new HashSet<string> { "kb-cert-iso27001" },
            Sections =
            [
                new DraftSection { Name = "Solution", Text = "We commit to a 99.9% SLA." },
            ],
            FormatRules = new SubmissionFormatRules
            {
                RequiredSections = ["Solution", "Commercials"],
                MaxPages = 40,
            },
            PageCount = 12,
            ConsistencyFigures =
            [
                new ConsistencyFigure
                {
                    Name = "total-effort-hours",
                    Values = new Dictionary<string, double> { ["plan"] = 1200, ["estimate"] = 1500 },
                },
            ],
        };

        var report = pipeline.Review(context);

        Assert.Contains(report.Findings, f => f.Kind == ReviewFindingKind.CoverageGap);
        Assert.Contains(report.Findings, f => f.Kind == ReviewFindingKind.ContractualCommitment);
        Assert.Contains(report.Findings, f => f.Kind == ReviewFindingKind.FormatViolation);
        Assert.Contains(report.Findings, f => f.Kind == ReviewFindingKind.Inconsistency);
        Assert.DoesNotContain(report.Findings, f => f.Kind == ReviewFindingKind.UnsupportedClaim);
        Assert.True(report.HasCriticalFindings);
    }
}
