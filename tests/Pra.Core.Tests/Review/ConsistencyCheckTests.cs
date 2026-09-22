using Pra.Core.Review;
using Xunit;

namespace Pra.Core.Tests.Review;

public class ConsistencyCheckTests
{
    private readonly ConsistencyCheck _check = new();

    [Fact]
    public void Disagreeing_figures_are_flagged()
    {
        var context = new ReviewContext
        {
            ConsistencyFigures =
            [
                new ConsistencyFigure
                {
                    Name = "total-effort-hours",
                    Values = new Dictionary<string, double>
                    {
                        ["plan"] = 1200,
                        ["estimate"] = 1500,
                    },
                },
            ],
        };

        var finding = Assert.Single(_check.Run(context));
        Assert.Equal(ReviewFindingKind.Inconsistency, finding.Kind);
        Assert.Equal(ReviewSeverity.Warning, finding.Severity);
        Assert.Equal("total-effort-hours", finding.Location);
        Assert.Contains("plan = 1200", finding.Description);
        Assert.Contains("estimate = 1500", finding.Description);
    }

    [Fact]
    public void Agreeing_figures_produce_no_findings()
    {
        var context = new ReviewContext
        {
            ConsistencyFigures =
            [
                new ConsistencyFigure
                {
                    Name = "duration-weeks",
                    Values = new Dictionary<string, double>
                    {
                        ["plan"] = 26,
                        ["narrative"] = 26,
                    },
                },
            ],
        };

        Assert.Empty(_check.Run(context));
    }

    [Fact]
    public void Figure_with_a_single_value_is_not_flagged()
    {
        var context = new ReviewContext
        {
            ConsistencyFigures =
            [
                new ConsistencyFigure
                {
                    Name = "team-size",
                    Values = new Dictionary<string, double> { ["plan"] = 6 },
                },
            ],
        };

        Assert.Empty(_check.Run(context));
    }
}
