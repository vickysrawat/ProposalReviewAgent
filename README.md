# ProposalReviewAgent

Implementation of the [Agentic AI Governance, Security & Compliance plan](agentic-ai-governance-security-compliance-plan.md), starting with the **PRA-Core** review-agent skeleton from the Foundation phase of the 90-day rollout.

## What's implemented

`src/Pra.Core` — the governance skeleton; nothing here talks to external services yet.

| Namespace | Contents | Plan section |
| --- | --- | --- |
| `Pra.Core.Governance` | Agent registry (single source of truth), registration records mirroring the registry schema, risk-tier rubric, tier-derived runtime controls | §2–§4 |
| `Pra.Core.Policy` | Pre-action policy engine: allow / block / escalate decisions for tool calls, with every decision appended to an audit log | §5 |
| `Pra.Core.Llm` | `ILlmProvider` abstraction and task-class-based `ModelRouter` that only resolves registry-approved deployments | §11.3 |
| `Pra.Core.Review` | PRA-Core critic pipeline: compliance-matrix coverage check, anti-hallucination citation check, contractual-commitment detection, plan/estimate/narrative consistency check, submission-format rules, finding/report types, 3-iteration writer loop cap | §11.9 |

`tests/Pra.Core.Tests` — xunit coverage for registry round-trips, tier → control mapping, policy decisions (allow-list, data labels, HITL escalation, suspended agents, audit logging), model routing, and the review pipeline (coverage, citation grounding, commitment language, format rules, consistency).

## Design invariants

- An agent takes the **highest tier** any single rubric factor puts it in.
- Runtime controls are **derived from the tier**, never hardcoded per agent.
- Tools not on the agent's allow-list are **blocked**; write/external actions **escalate to HITL** per tier; suspended or retired agents cannot invoke tools.
- Every policy decision is logged — a blocked call is also an audit record.
- Claims must cite a knowledge-base source; unsupported claims are flagged, not written.

## Build and test

```bash
dotnet build
dotnet test
```

## Next steps (per the rollout plan)

1. Persist the registry and decision log; wire OTel GenAI tracing spans.
2. Route tool calls through the APIM gateway with per-agent identities.
3. Replace the deterministic review checks with LLM-backed implementations behind `IReviewCheck`, keeping the deterministic ones as a fast pre-pass.
4. Scaffold the RFP-to-proposal orchestrator (Intake → Requirements → … → Review) once the governance plane is live.
