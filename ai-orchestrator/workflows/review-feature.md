Review the requested feature, plan, or implementation using a controlled multi-agent workflow.

---

## Step 1 — Act as Manager

Identify review target:
- feature request
- implementation plan
- code change / diff
- workflow / architecture review

Identify affected areas:
- backend
- frontend
- database
- auth
- integration
- test

---

## Step 2 — Select Agents (strict)

Call ONLY the necessary agents:

- Backend Agent → if backend/API/domain/db/auth/integration is involved
- Frontend Agent → if UI/flow/components/state/API usage is involved
- QA-Reviewer Agent → for validation, regression, edge cases, and final critique
- Architect Agent → ONLY if:
  - cross-layer impact
  - schema change
  - external integration
  - auth change
  - state/flow change
  - breaking change risk

If not clearly needed → DO NOT call

---

## Step 3 — Review Focus

Check only:
- correctness
- scope control
- consistency with existing patterns
- regression risk
- missing validation
- permission/auth gaps
- failure handling
- overengineering
- contradictions

Do NOT redesign the whole system unless required.

---

## Step 4 — Merge Outputs

Combine findings into one final review.

Remove:
- duplicate comments
- vague remarks
- unnecessary scope expansion

Prioritize:
1. critical risks
2. correctness issues
3. regression risks
4. simplification opportunities

---

## Output (concise)

## Review Target
...

## Affected Areas
- ...

## Selected Agents
- ...

## Strengths
- ...

## Problems
- ...

## Risks
- ...

## Missing Checks
- ...

## Required Fixes
- ...

## Final Verdict
Acceptable / Needs Changes / Risky / Reject

---

## Rules

- Keep output concise (max ~300 words unless complex)
- Do NOT generate code unless explicitly asked
- Do NOT approve weak solutions
- Prefer direct, actionable criticism