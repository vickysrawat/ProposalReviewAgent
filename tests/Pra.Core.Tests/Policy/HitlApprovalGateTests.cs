using Pra.Core.Policy;
using Xunit;

namespace Pra.Core.Tests.Policy;

public class HitlApprovalGateTests
{
    private static PolicyDecision Escalation() =>
        PolicyDecision.Escalate("pra-core-reviewer", "email.send", "External action requires approval.", "POL-HITL-EXT-COMMS");

    [Fact]
    public void Escalation_opens_a_pending_request()
    {
        var gate = new HitlApprovalGate();

        var request = gate.RequestApproval(Escalation());

        Assert.Equal(ApprovalStatus.Pending, request.Status);
        Assert.False(gate.IsApproved(request.RequestId));
        Assert.Single(gate.ApprovalLog);
    }

    [Fact]
    public void Approval_is_recorded_with_approver_and_rationale()
    {
        var gate = new HitlApprovalGate();
        var request = gate.RequestApproval(Escalation());

        var decision = gate.Decide(request.RequestId, approved: true, approver: "bid.director@contoso.com", rationale: "Client expects the email.");

        Assert.Equal(ApprovalStatus.Approved, decision.Status);
        Assert.Equal("bid.director@contoso.com", decision.Approver);
        Assert.True(gate.IsApproved(request.RequestId));
        Assert.Equal(2, gate.ApprovalLog.Count);
    }

    [Fact]
    public void Rejection_is_not_an_approval()
    {
        var gate = new HitlApprovalGate();
        var request = gate.RequestApproval(Escalation());

        gate.Decide(request.RequestId, approved: false, approver: "risk@contoso.com");

        Assert.False(gate.IsApproved(request.RequestId));
    }

    [Fact]
    public void Non_escalated_decision_cannot_open_a_request()
    {
        var gate = new HitlApprovalGate();

        Assert.Throws<ArgumentException>(
            () => gate.RequestApproval(PolicyDecision.Allow("a", "t", "fine")));
    }

    [Fact]
    public void Deciding_an_unknown_request_throws()
    {
        var gate = new HitlApprovalGate();

        Assert.Throws<KeyNotFoundException>(() => gate.Decide("nope", true, "someone"));
    }

    [Fact]
    public void Requests_and_decisions_land_in_the_audit_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pra-hitl-{Guid.NewGuid():N}.jsonl");
        try
        {
            var gate = new HitlApprovalGate(auditPath: path);
            var request = gate.RequestApproval(Escalation());
            gate.Decide(request.RequestId, approved: true, approver: "bid.director@contoso.com");

            var persisted = Pra.Core.Persistence.JsonLinesStore.ReadAll<ApprovalEvent>(path);
            Assert.Equal(2, persisted.Count);
            Assert.Equal(ApprovalStatus.Pending, persisted[0].Status);
            Assert.Equal(ApprovalStatus.Approved, persisted[1].Status);
            Assert.Equal(persisted[0].RequestId, persisted[1].RequestId);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
