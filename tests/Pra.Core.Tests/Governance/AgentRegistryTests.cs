using Pra.Core.Governance;
using Xunit;

namespace Pra.Core.Tests.Governance;

public class AgentRegistryTests
{
    private static AgentRegistration Sample(string agentId = "pra-core-reviewer") => new()
    {
        AgentId = agentId,
        Version = "1.0.0",
        DisplayName = "Proposal Review Agent",
        Purpose = "Reviews RFP responses against client requirements",
        Owners = new AgentOwners
        {
            Business = "owner@contoso.com",
            Technical = "techlead@contoso.com",
            Risk = "risk@contoso.com",
        },
        RiskTier = RiskTier.Medium,
        AutonomyLevel = AutonomyLevel.L1,
        LastReviewed = new DateOnly(2026, 9, 21),
    };

    [Fact]
    public void Register_then_get_round_trips()
    {
        var registry = new AgentRegistry();
        registry.Register(Sample());

        var loaded = registry.Get("pra-core-reviewer");

        Assert.Equal("Proposal Review Agent", loaded.DisplayName);
    }

    [Fact]
    public void Register_duplicate_agent_id_throws()
    {
        var registry = new AgentRegistry();
        registry.Register(Sample());

        Assert.Throws<InvalidOperationException>(() => registry.Register(Sample()));
    }

    [Fact]
    public void Get_unknown_agent_throws()
    {
        var registry = new AgentRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.Get("nope"));
    }

    [Fact]
    public void Update_replaces_existing_record()
    {
        var registry = new AgentRegistry();
        registry.Register(Sample());
        registry.Update(Sample() with { Status = AgentStatus.Production });

        Assert.Equal(AgentStatus.Production, registry.Get("pra-core-reviewer").Status);
    }
}
