namespace Pra.Core.Policy;

/// <summary>The approver's recorded verdict on an escalated tool call.</summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>
/// A human-approval event: who decided, when, and why. Every event is an
/// audit record (plan, section 5 sequence diagram: approval request and
/// approval decision are both logged).
/// </summary>
public sealed record ApprovalEvent
{
    public required string RequestId { get; init; }
    public required string AgentId { get; init; }
    public required string ToolName { get; init; }
    public required ApprovalStatus Status { get; init; }
    public string? Approver { get; init; }
    public string? Rationale { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// The HITL side of the pre-action filter (plan, section 5). A policy
/// escalation opens an approval request; the call proceeds only after a
/// recorded approval. Requests and decisions are kept in append-only logs so
/// an auditor can answer "who approved this external send?" from the trail.
/// </summary>
public sealed class HitlApprovalGate
{
    private readonly string? _auditPath;
    private readonly List<ApprovalEvent> _events = [];
    private readonly object _lock = new();

    public HitlApprovalGate(string? auditPath = null)
    {
        _auditPath = auditPath;
    }

    /// <summary>Opens an approval request for an escalated decision.</summary>
    public ApprovalEvent RequestApproval(PolicyDecision escalation)
    {
        ArgumentNullException.ThrowIfNull(escalation);

        if (escalation.Kind != PolicyDecisionKind.Escalate)
            throw new ArgumentException("Only escalated decisions can be escalated to HITL.", nameof(escalation));

        using var span = Pra.Core.Telemetry.PraTelemetry.StartApproval(escalation.AgentId, escalation.ToolName);
        span?.SetTag("pra.hitl.status", ApprovalStatus.Pending.ToString());

        var request = new ApprovalEvent
        {
            RequestId = Guid.NewGuid().ToString("N"),
            AgentId = escalation.AgentId,
            ToolName = escalation.ToolName,
            Status = ApprovalStatus.Pending,
        };

        Record(request);
        return request;
    }

    /// <summary>Records the approver's decision; the pending request must exist.</summary>
    public ApprovalEvent Decide(string requestId, bool approved, string approver, string? rationale = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approver);

        ApprovalEvent decision;
        lock (_lock)
        {
            var pending = _events.LastOrDefault(e => e.RequestId == requestId && e.Status == ApprovalStatus.Pending)
                ?? throw new KeyNotFoundException($"No pending approval request '{requestId}'.");

            decision = pending with
            {
                Status = approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected,
                Approver = approver,
                Rationale = rationale,
                Timestamp = DateTimeOffset.UtcNow,
            };
        }

        using var span = Pra.Core.Telemetry.PraTelemetry.StartApproval(decision.AgentId, decision.ToolName);
        span?.SetTag("pra.hitl.status", decision.Status.ToString());
        span?.SetTag("pra.hitl.approver", approver);

        Record(decision);
        return decision;
    }

    /// <summary>Whether the given request was approved.</summary>
    public bool IsApproved(string requestId)
    {
        lock (_lock)
        {
            return _events.Any(e => e.RequestId == requestId && e.Status == ApprovalStatus.Approved);
        }
    }

    /// <summary>The append-only approval trail, in order.</summary>
    public IReadOnlyList<ApprovalEvent> ApprovalLog
    {
        get
        {
            lock (_lock)
            {
                return _events.ToArray();
            }
        }
    }

    private void Record(ApprovalEvent approvalEvent)
    {
        lock (_lock)
        {
            _events.Add(approvalEvent);
        }

        if (_auditPath is not null)
            Pra.Core.Persistence.JsonLinesStore.Append(_auditPath, approvalEvent);
    }
}
