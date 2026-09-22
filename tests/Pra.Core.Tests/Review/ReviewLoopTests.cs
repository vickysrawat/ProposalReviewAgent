using Pra.Core.Review;
using Xunit;

namespace Pra.Core.Tests.Review;

public class ReviewLoopTests
{
    private static ReviewContext DraftWithGap() => new()
    {
        RequirementCoverage = new Dictionary<string, bool> { ["REQ-1"] = false },
    };

    private static ReviewContext CleanDraft() => new()
    {
        RequirementCoverage = new Dictionary<string, bool> { ["REQ-1"] = true },
    };

    [Fact]
    public void Clean_first_draft_is_accepted_in_one_iteration()
    {
        var loop = new ReviewLoop();

        var result = loop.Run(CleanDraft(), (draft, _) => draft);

        Assert.Equal(ReviewLoopOutcome.Accepted, result.Outcome);
        Assert.Single(result.Iterations);
        Assert.False(result.RequiresHumanIntervention);
    }

    [Fact]
    public void Writer_fixing_the_gap_within_budget_is_revised()
    {
        var loop = new ReviewLoop();

        var result = loop.Run(DraftWithGap(), (draft, _) => CleanDraft());

        Assert.Equal(ReviewLoopOutcome.Revised, result.Outcome);
        Assert.Equal(2, result.Iterations.Count);
        Assert.False(result.FinalReport.HasCriticalFindings);
    }

    [Fact]
    public void Persistent_critical_findings_escalate_after_three_iterations()
    {
        var loop = new ReviewLoop();
        var revisions = 0;

        var result = loop.Run(DraftWithGap(), (draft, _) =>
        {
            revisions++;
            return new ReviewContext
            {
                RequirementCoverage = new Dictionary<string, bool> { ["REQ-1"] = false },
            };
        });

        Assert.Equal(ReviewLoopOutcome.EscalatedToHuman, result.Outcome);
        Assert.True(result.RequiresHumanIntervention);
        Assert.Equal(ProposalReviewPipeline.MaxIterations, result.Iterations.Count);
        Assert.Equal(ProposalReviewPipeline.MaxIterations - 1, revisions);
        Assert.True(result.FinalReport.HasCriticalFindings);
    }

    [Fact]
    public void Writer_returning_same_draft_escalates_early_instead_of_burning_budget()
    {
        var loop = new ReviewLoop();
        var same = DraftWithGap();

        var result = loop.Run(same, (draft, _) => draft);

        Assert.Equal(ReviewLoopOutcome.EscalatedToHuman, result.Outcome);
        Assert.Single(result.Iterations);
    }

    [Fact]
    public void Iteration_cap_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReviewLoop(maxIterations: 0));
    }
}
