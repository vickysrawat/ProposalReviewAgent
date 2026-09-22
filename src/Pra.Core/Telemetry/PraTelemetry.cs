using System.Diagnostics;

namespace Pra.Core.Telemetry;

/// <summary>
/// OpenTelemetry tracing entry points for PRA-Core (plan, section 6): one
/// <see cref="ActivitySource"/> whose spans follow the OTel GenAI semantic
/// conventions, so an auditor can reconstruct "why did the agent do X?" from
/// traces. All helpers are no-ops until a listener (OTel SDK, exporter) is
/// attached, so instrumentation costs nothing in tests.
/// </summary>
public static class PraTelemetry
{
    public const string SourceName = "Pra.Core";

    public static readonly ActivitySource Source = new(SourceName);

    /// <summary>Starts a span for a pre-action policy decision.</summary>
    public static Activity? StartPolicyDecision(string agentId, string toolName) =>
        Source.StartActivity("pra.policy.decision", ActivityKind.Internal)?
            .SetTag("gen_ai.agent.id", agentId)
            .SetTag("pra.tool.name", toolName);

    /// <summary>Starts a span for a review pass over a proposal draft.</summary>
    public static Activity? StartReviewPass(string? agentId = null) =>
        Source.StartActivity("pra.review.pass", ActivityKind.Internal)?
            .SetTag("gen_ai.agent.id", agentId ?? "pra-core-reviewer");

    /// <summary>Starts a span for one writer↔review iteration.</summary>
    public static Activity? StartReviewIteration(int iteration) =>
        Source.StartActivity("pra.review.iteration", ActivityKind.Internal)?
            .SetTag("pra.review.iteration", iteration);

    /// <summary>Starts a span for an LLM completion routed through the gateway.</summary>
    public static Activity? StartCompletion(string agentId, string taskClass, string deployment) =>
        Source.StartActivity("pra.llm.completion", ActivityKind.Client)?
            .SetTag("gen_ai.agent.id", agentId)
            .SetTag("gen_ai.request.task_class", taskClass)
            .SetTag("gen_ai.request.model", deployment);

    /// <summary>Starts a span for a HITL approval request/decision.</summary>
    public static Activity? StartApproval(string agentId, string toolName) =>
        Source.StartActivity("pra.hitl.approval", ActivityKind.Internal)?
            .SetTag("gen_ai.agent.id", agentId)
            .SetTag("pra.tool.name", toolName);
}
