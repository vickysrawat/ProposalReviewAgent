# ProposalReviewAgent

Implementation of the [Agentic AI Governance, Security & Compliance plan](agentic-ai-governance-security-compliance-plan.md), starting with the **PRA-Core** review-agent skeleton from the Foundation phase of the 90-day rollout.

## What's implemented

`src/Pra.Core` — the governance skeleton; registry, audit logs and review loops are in place; nothing here talks to external services yet.

| Namespace | Contents | Plan section |
| --- | --- | --- |
| `Pra.Core.Governance` | Agent registry (single source of truth, JSONL-snapshot persistence), registration records mirroring the registry schema, risk-tier rubric, tier-derived runtime controls | §2–§4 |
| `Pra.Core.Policy` | Pre-action policy engine: allow / block / escalate decisions for tool calls, blast-radius rate limits, block-and-log for unregistered agents, HITL approval gate, every decision and approval appended to an audit log (optional JSONL sink) | §5 |
| `Pra.Core.Llm` | `ILlmProvider` abstraction and task-class-based `ModelRouter` that only resolves registry-approved deployments and never falls back to an arbitrary model | §11.3 |
| `Pra.Core.Review` | PRA-Core critic pipeline: compliance-matrix coverage check, anti-hallucination citation check, contractual-commitment detection, plan/estimate/narrative consistency check, submission-format rules (sections, page/word limits, deadline), finding/report types, and the writer↔review loop with 3-iteration cap and human escalation | §11.9 |
| `Pra.Core.Telemetry` | `PraTelemetry` activity source with OTel GenAI span helpers for policy decisions, review passes, review iterations, completions and approvals; no-op until a listener is attached | §6 |
| `Pra.Core.Persistence` | JSON Lines store used to persist registry snapshots and append-only decision/approval audit logs | §6 |

`tests/Pra.Core.Tests` — xunit coverage for registry round-trips, tier → control mapping, policy decisions (allow-list, data labels, HITL escalation, suspended agents, audit logging), model routing, and the review pipeline (coverage, citation grounding, commitment language, format rules, consistency).

## Design invariants

- An agent takes the **highest tier** any single rubric factor puts it in.
- Runtime controls are **derived from the tier**, never hardcoded per agent.
- Tools not on the agent's allow-list are **blocked**; write/external actions **escalate to HITL** per tier; suspended, retired or **unregistered** agents cannot invoke tools — and the attempt is still an audit record, not an exception.
- Blast-radius limits are enforced at the pre-action filter: an agent over its per-minute tool-call budget is blocked.
- Escalations open a recorded HITL approval request; the tool call proceeds only after a recorded approval with approver and rationale.
- Every policy decision and approval is logged — a blocked call is also an audit record — and lands in append-only storage when an audit sink is configured.
- Claims must cite a knowledge-base source; unsupported claims are flagged, not written. The citation is the **last** `[source-id]` marker, so prose brackets like `[Appendix B]` don't mask it.
- Findings loop back to the Writer at most **3 times**; persistent critical findings escalate to a human.
- Every policy decision, review pass, completion and approval emits an OTel span on the `Pra.Core` activity source.

## Build and test

```bash
dotnet build
dotnet test
```

## Next steps (per the rollout plan)

1. Replace the JSONL persistence with immutable blob / Log Analytics; attach an OTel exporter to the `Pra.Core` source and carry one trace ID per RFP across agents.
2. Route tool calls through the APIM gateway with per-agent identities.
3. Replace the deterministic review checks with LLM-backed implementations behind `IReviewCheck`, keeping the deterministic ones as a fast pre-pass; add groundedness scoring so a citation is checked for actually supporting its claim.
4. Add input guardrails (Prompt Shields on untrusted RFP chunks) and output guardrails (PII redaction, content safety) around the agents.
5. Scaffold the RFP-to-proposal orchestrator (Intake → Requirements → … → Review) once the governance plane is live.
