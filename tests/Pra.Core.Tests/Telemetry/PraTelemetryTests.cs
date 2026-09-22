using System.Diagnostics;
using Pra.Core.Telemetry;
using Xunit;

namespace Pra.Core.Tests.Telemetry;

public class PraTelemetryTests
{
    [Fact]
    public void Spans_are_noops_without_a_listener()
    {
        using var span = PraTelemetry.StartPolicyDecision("agent", "tool");

        Assert.Null(span);
    }

    [Fact]
    public void Policy_span_carries_genai_tags_when_listened()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == PraTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => activities.Add(a),
        };
        ActivitySource.AddActivityListener(listener);

        using (PraTelemetry.StartPolicyDecision("pra-core-reviewer", "email.send"))
        {
        }

        // Other tests running in parallel may also emit spans on this shared source.
        var activity = Assert.Single(activities, a => a.OperationName == "pra.policy.decision");
        Assert.Contains(activity.TagObjects, t => t.Key == "gen_ai.agent.id" && (string?)t.Value == "pra-core-reviewer");
        Assert.Contains(activity.TagObjects, t => t.Key == "pra.tool.name" && (string?)t.Value == "email.send");
    }
}
