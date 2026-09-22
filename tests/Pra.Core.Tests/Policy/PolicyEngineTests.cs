using Pra.Core.Governance;
using Pra.Core.Policy;
using Xunit;

namespace Pra.Core.Tests.Policy;

public class PolicyEngineTests
{
    private static AgentOwners Owners() => new() { Business = "b", Technical = "t", Risk = "r" };

    private static AgentRegistration AgentWithTools(RiskTier tier, params ToolGrant[] tools) => new()
    {
        AgentId = "pra-core-reviewer",
        Version = "1.0.0",
        DisplayName = "Proposal Review Agent",
        Purpose = "test",
        Owners = Owners(),
        RiskTier = tier,
        AutonomyLevel = AutonomyLevel.L2,
        Status = AgentStatus.Production,
        Tools = tools,
        AllowedDataLabels = ["General", "Confidential"],
        BlockedDataLabels = ["Highly Confidential"],
        LastReviewed = new DateOnly(2026, 9, 21),
    };

    private static ToolCallRequest Call(string tool, params string[] labels) =>
        new() { AgentId = "pra-core-reviewer", ToolName = tool, DataLabels = labels };

    [Fact]
    public void Allow_listed_read_tool_on_allowed_label_is_allowed()
    {
        var engine = new PolicyEngine(new AgentRegistry());
        var registry = new AgentRegistry();
        engine = new PolicyEngine(registry);
        registry.Register(AgentWithTools(RiskTier.Medium,
            new ToolGrant { Name = "sharepoint.search", Scope = "read" }));

        var decision = engine.Evaluate(Call("sharepoint.search", "Confidential"));

        Assert.Equal(PolicyDecisionKind.Allow, decision.Kind);
    }

    [Fact]
    public void Tool_not_on_allow_list_is_blocked()
    {
        var registry = new AgentRegistry();
        registry.Register(AgentWithTools(RiskTier.Low));
        var engine = new PolicyEngine(registry);

        var decision = engine.Evaluate(Call("email.send"));

        Assert.Equal(PolicyDecisionKind.Block, decision.Kind);
        Assert.Equal("POL-TOOL-ALLOWLIST", decision.PolicyId);
    }

    [Fact]
    public void Blocked_data_label_is_blocked()
    {
        var registry = new AgentRegistry();
        registry.Register(AgentWithTools(RiskTier.High,
            new ToolGrant { Name = "pricing.read", Scope = "read" }));
        var engine = new PolicyEngine(registry);

        var decision = engine.Evaluate(Call("pricing.read", "Highly Confidential"));

        Assert.Equal(PolicyDecisionKind.Block, decision.Kind);
    }

    [Fact]
    public void Write_tool_at_medium_tier_escalates_to_hitl()
    {
        var registry = new AgentRegistry();
        registry.Register(AgentWithTools(RiskTier.Medium,
            new ToolGrant { Name = "doc.write", Scope = "write" }));
        var engine = new PolicyEngine(registry);

        var decision = engine.Evaluate(Call("doc.write", "Confidential"));

        Assert.Equal(PolicyDecisionKind.Escalate, decision.Kind);
    }

    [Fact]
    public void Tool_flagged_requires_approval_always_escalates()
    {
        var registry = new AgentRegistry();
        registry.Register(AgentWithTools(RiskTier.Low,
            new ToolGrant { Name = "email.send", Scope = "write", RequiresApproval = true, External = true }));
        var engine = new PolicyEngine(registry);

        var decision = engine.Evaluate(Call("email.send", "General"));

        Assert.Equal(PolicyDecisionKind.Escalate, decision.Kind);
        Assert.Equal("POL-HITL-EXT-COMMS", decision.PolicyId);
    }

    [Fact]
    public void Suspended_agent_cannot_invoke_tools()
    {
        var registry = new AgentRegistry();
        var suspended = new AgentRegistration
        {
            AgentId = "pra-core-reviewer",
            Version = "1.0.0",
            DisplayName = "Proposal Review Agent",
            Purpose = "test",
            Owners = Owners(),
            RiskTier = RiskTier.Low,
            AutonomyLevel = AutonomyLevel.L2,
            Status = AgentStatus.Suspended,
            Tools = [new ToolGrant { Name = "sharepoint.search", Scope = "read" }],
            LastReviewed = new DateOnly(2026, 9, 21),
        };
        registry.Register(suspended);
        var engine = new PolicyEngine(registry);

        var decision = engine.Evaluate(Call("sharepoint.search"));

        Assert.Equal(PolicyDecisionKind.Block, decision.Kind);
    }

    [Fact]
    public void Every_decision_is_logged_as_audit_record()
    {
        var registry = new AgentRegistry();
        registry.Register(AgentWithTools(RiskTier.Low,
            new ToolGrant { Name = "sharepoint.search", Scope = "read" }));
        var engine = new PolicyEngine(registry);

        engine.Evaluate(Call("sharepoint.search", "General"));
        engine.Evaluate(Call("email.send"));

        Assert.Equal(2, engine.DecisionLog.Count);
        Assert.Equal(PolicyDecisionKind.Allow, engine.DecisionLog[0].Kind);
        Assert.Equal(PolicyDecisionKind.Block, engine.DecisionLog[1].Kind);
    }
}
