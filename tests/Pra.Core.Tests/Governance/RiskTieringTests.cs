using Pra.Core.Governance;
using Xunit;

namespace Pra.Core.Tests.Governance;

public class RiskTieringTests
{
    private static RiskFactorAssessment AllLow() => new()
    {
        DataTouched = RiskTier.Low,
        Actions = RiskTier.Low,
        Autonomy = RiskTier.Low,
        AudienceImpact = RiskTier.Low,
        RegulatoryExposure = RiskTier.Low,
    };

    [Fact]
    public void Overall_tier_is_highest_single_factor()
    {
        Assert.Equal(RiskTier.Low, AllLow().OverallTier);

        var highActions = new RiskFactorAssessment
        {
            DataTouched = RiskTier.Low,
            Actions = RiskTier.High,
            Autonomy = RiskTier.Low,
            AudienceImpact = RiskTier.Low,
            RegulatoryExposure = RiskTier.Low,
        };
        Assert.Equal(RiskTier.High, highActions.OverallTier);

        var mediumAutonomy = new RiskFactorAssessment
        {
            DataTouched = RiskTier.Low,
            Actions = RiskTier.Low,
            Autonomy = RiskTier.Medium,
            AudienceImpact = RiskTier.Low,
            RegulatoryExposure = RiskTier.Low,
        };
        Assert.Equal(RiskTier.Medium, mediumAutonomy.OverallTier);
    }

    [Fact]
    public void Low_tier_has_no_hitl_and_lightweight_path()
    {
        var controls = RuntimeControls.ForTier(RiskTier.Low);

        Assert.False(controls.HumanApprovalOnWrites);
        Assert.False(controls.RequiresBoardApproval);
        Assert.Equal(TimeSpan.FromDays(90), controls.TraceRetention);
    }

    [Fact]
    public void Medium_tier_requires_hitl_on_writes()
    {
        var controls = RuntimeControls.ForTier(RiskTier.Medium);

        Assert.True(controls.HumanApprovalOnWrites);
        Assert.False(controls.RequiresBoardApproval);
    }

    [Fact]
    public void High_tier_requires_board_approval_and_quarterly_access_review()
    {
        var controls = RuntimeControls.ForTier(RiskTier.High);

        Assert.True(controls.RequiresBoardApproval);
        Assert.True(controls.RedTeamBeforeRelease);
        Assert.Equal(TimeSpan.FromDays(91), controls.AccessReviewCadence);
    }

    [Fact]
    public void Registration_derives_controls_from_tier()
    {
        var agent = new AgentRegistration
        {
            AgentId = "a1",
            Version = "1.0.0",
            DisplayName = "A1",
            Purpose = "test",
            Owners = new AgentOwners { Business = "b", Technical = "t", Risk = "r" },
            RiskTier = RiskTier.High,
            AutonomyLevel = AutonomyLevel.L2,
            LastReviewed = new DateOnly(2026, 9, 21),
        };

        Assert.True(agent.Controls.RequiresBoardApproval);
    }
}
