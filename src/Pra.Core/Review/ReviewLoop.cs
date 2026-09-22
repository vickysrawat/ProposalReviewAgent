namespace Pra.Core.Review;

/// <summary>How a review loop ended.</summary>
public enum ReviewLoopOutcome
{
    /// <summary>The draft passed review within the iteration budget.</summary>
    Accepted,

    /// <summary>The writer fixed every critical finding within the iteration budget.</summary>
    Revised,

    /// <summary>Critical findings remained after the iteration cap; a human must take over.</summary>
    EscalatedToHuman,
}

/// <summary>One iteration of the writer/review loop, kept for the audit trail.</summary>
public sealed record ReviewIteration
{
    public required int Number { get; init; }
    public required ReviewContext Draft { get; init; }
    public required ReviewReport Report { get; init; }
}

/// <summary>Final result of a review loop run.</summary>
public sealed record ReviewLoopResult
{
    public required ReviewLoopOutcome Outcome { get; init; }
    public required ReviewContext FinalDraft { get; init; }
    public required ReviewReport FinalReport { get; init; }
    public required IReadOnlyList<ReviewIteration> Iterations { get; init; }

    /// <summary>True when a human must resolve the remaining findings (plan, section 11.9).</summary>
    public bool RequiresHumanIntervention => Outcome == ReviewLoopOutcome.EscalatedToHuman;
}

/// <summary>
/// The writer↔review loop from plan section 11.9: findings loop back to the
/// Writer, with a hard maximum of 3 iterations before the run escalates to a
/// human. The loop only coordinates — the caller supplies the writer that
/// revises a draft in response to findings, so an LLM-backed writer can be
/// plugged in without changing the loop.
/// </summary>
public sealed class ReviewLoop
{
    private readonly ProposalReviewPipeline _pipeline;
    private readonly int _maxIterations;

    public ReviewLoop(ProposalReviewPipeline? pipeline = null, int maxIterations = ProposalReviewPipeline.MaxIterations)
    {
        if (maxIterations < 1)
            throw new ArgumentOutOfRangeException(nameof(maxIterations), maxIterations, "At least one iteration is required.");

        _pipeline = pipeline ?? new ProposalReviewPipeline();
        _maxIterations = maxIterations;
    }

    /// <summary>Runs the loop until the draft is clean, the budget is spent, or the writer stops revising.</summary>
    public ReviewLoopResult Run(
        ReviewContext initialDraft,
        Func<ReviewContext, ReviewReport, ReviewContext> revise)
    {
        ArgumentNullException.ThrowIfNull(initialDraft);
        ArgumentNullException.ThrowIfNull(revise);

        var iterations = new List<ReviewIteration>();
        var draft = initialDraft;

        for (var i = 1; ; i++)
        {
            using var span = Pra.Core.Telemetry.PraTelemetry.StartReviewIteration(i);

            var report = _pipeline.Review(draft);
            iterations.Add(new ReviewIteration { Number = i, Draft = draft, Report = report });

            if (!report.HasCriticalFindings)
            {
                return new ReviewLoopResult
                {
                    Outcome = i == 1 ? ReviewLoopOutcome.Accepted : ReviewLoopOutcome.Revised,
                    FinalDraft = draft,
                    FinalReport = report,
                    Iterations = iterations,
                };
            }

            if (i >= _maxIterations)
            {
                return new ReviewLoopResult
                {
                    Outcome = ReviewLoopOutcome.EscalatedToHuman,
                    FinalDraft = draft,
                    FinalReport = report,
                    Iterations = iterations,
                };
            }

            var revised = revise(draft, report);

            // The writer returned the same draft: looping would not change the
            // outcome, so escalate rather than burn the remaining budget.
            if (ReferenceEquals(revised, draft))
            {
                return new ReviewLoopResult
                {
                    Outcome = ReviewLoopOutcome.EscalatedToHuman,
                    FinalDraft = draft,
                    FinalReport = report,
                    Iterations = iterations,
                };
            }

            draft = revised;
        }
    }
}
