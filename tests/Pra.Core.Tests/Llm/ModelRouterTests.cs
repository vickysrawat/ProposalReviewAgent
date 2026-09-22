using Pra.Core.Governance;
using Pra.Core.Llm;
using Xunit;

namespace Pra.Core.Tests.Llm;

public class ModelRouterTests
{
    private static AgentRegistry RegistryWithModels(params string[] deployments)
    {
        var registry = new AgentRegistry();
        registry.Register(new AgentRegistration
        {
            AgentId = "pra-core-reviewer",
            Version = "1.0.0",
            DisplayName = "PRA",
            Purpose = "test",
            Owners = new AgentOwners { Business = "b", Technical = "t", Risk = "r" },
            RiskTier = RiskTier.Medium,
            AutonomyLevel = AutonomyLevel.L1,
            Models = deployments
                .Select(d => new ApprovedModel { Provider = "azure-openai", Deployment = d })
                .ToArray(),
            LastReviewed = new DateOnly(2026, 9, 21),
        });
        return registry;
    }

    [Fact]
    public void Parsing_routes_to_cheap_deployment()
    {
        var router = new ModelRouter(RegistryWithModels("gpt-cheap-mini", "gpt-strong-pro"));

        var model = router.Resolve("pra-core-reviewer", ModelTaskClass.Parsing);

        Assert.Equal("gpt-cheap-mini", model.Deployment);
    }

    [Fact]
    public void Writing_routes_to_strong_deployment()
    {
        var router = new ModelRouter(RegistryWithModels("gpt-cheap-mini", "gpt-strong-pro"));

        var model = router.Resolve("pra-core-reviewer", ModelTaskClass.Writing);

        Assert.Equal("gpt-strong-pro", model.Deployment);
    }

    [Fact]
    public void Agent_with_no_approved_models_throws()
    {
        var router = new ModelRouter(RegistryWithModels());

        Assert.Throws<InvalidOperationException>(
            () => router.Resolve("pra-core-reviewer", ModelTaskClass.Review));
    }

    [Fact]
    public void Unregistered_agent_throws()
    {
        var router = new ModelRouter(new AgentRegistry());

        Assert.Throws<KeyNotFoundException>(
            () => router.Resolve("ghost", ModelTaskClass.Parsing));
    }

    [Fact]
    public void No_matching_deployment_for_task_class_throws_instead_of_falling_back()
    {
        var router = new ModelRouter(RegistryWithModels("gpt-strong-pro"));

        var ex = Assert.Throws<InvalidOperationException>(
            () => router.Resolve("pra-core-reviewer", ModelTaskClass.Parsing));
        Assert.Contains("cheap", ex.Message);
    }
}
