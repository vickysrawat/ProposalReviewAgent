using Pra.Core.Review;
using Xunit;

namespace Pra.Core.Tests.Review;

public class FormatComplianceCheckTests
{
    private readonly FormatComplianceCheck _check = new();

    [Fact]
    public void Missing_required_section_is_critical()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules
            {
                RequiredSections = ["Executive Summary", "Solution", "Commercials"],
            },
            Sections =
            [
                new DraftSection { Name = "Executive Summary", Text = "…" },
                new DraftSection { Name = "Solution", Text = "…" },
            ],
        };

        var findings = _check.Run(context);

        var finding = Assert.Single(findings);
        Assert.Equal(ReviewFindingKind.FormatViolation, finding.Kind);
        Assert.Equal(ReviewSeverity.Critical, finding.Severity);
        Assert.Contains("Commercials", finding.Description);
    }

    [Fact]
    public void Required_section_match_is_case_insensitive()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules { RequiredSections = ["Executive Summary"] },
            Sections = [new DraftSection { Name = "executive summary", Text = "…" }],
        };

        Assert.Empty(_check.Run(context));
    }

    [Fact]
    public void Exceeding_page_limit_is_critical()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules { MaxPages = 40 },
            PageCount = 42,
        };

        var finding = Assert.Single(_check.Run(context));
        Assert.Equal(ReviewSeverity.Critical, finding.Severity);
        Assert.Contains("42", finding.Description);
        Assert.Contains("40", finding.Description);
    }

    [Fact]
    public void Exceeding_word_limit_is_critical()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules { MaxWords = 10_000 },
            WordCount = 12_500,
        };

        var finding = Assert.Single(_check.Run(context));
        Assert.Contains("word", finding.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compliant_draft_produces_no_findings()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules
            {
                RequiredSections = ["Solution"],
                MaxPages = 40,
                MaxWords = 10_000,
            },
            Sections = [new DraftSection { Name = "Solution", Text = "…" }],
            PageCount = 40,
            WordCount = 9_999,
        };

        Assert.Empty(_check.Run(context));
    }

    [Fact]
    public void Passed_submission_deadline_is_critical()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules
            {
                SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
            },
        };

        var finding = Assert.Single(_check.Run(context));
        Assert.Equal(ReviewSeverity.Critical, finding.Severity);
        Assert.Contains("deadline", finding.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Future_deadline_produces_no_finding()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules
            {
                SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            },
        };

        Assert.Empty(_check.Run(context));
    }

    [Fact]
    public void Word_count_falls_back_to_section_text_when_not_set()
    {
        var context = new ReviewContext
        {
            FormatRules = new SubmissionFormatRules { MaxWords = 3 },
            Sections = [new DraftSection { Name = "Solution", Text = "one two three four five" }],
        };

        var finding = Assert.Single(_check.Run(context));
        Assert.Contains("5", finding.Description);
    }
}
