Create an implementation plan using a controlled multi-agent workflow.

---

## Step 1 — Act as Manager

Classify the task:
- feature / bugfix / refactor / audit / review

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

- Backend Agent → if backend logic, API, DB, validation, auth, or integration is involved
- Frontend Agent → if UI, flow, components, state, or API usage is involved
- QA-Reviewer Agent → if validation, risks, edge cases, regression, or final critique is needed
- Architect Agent → ONLY if:
  - cross-layer impact
  - schema change
  - external integration
  - auth change
  - state/flow change
  - breaking change risk

If not clearly needed → DO NOT call

---

## Step 3 — Execution Strategy

Decide:

- Sequential → dependent tasks
- Parallel → independent analysis (for example backend + frontend)

Keep execution minimal.

---

## Step 4 — Merge Outputs

Combine results into a single plan.

Remove:
- duplicates
- contradictions
- unnecessary scope

Prioritize:
1. correctness
2. safety
3. minimal scope
4. consistency with existing patterns

---

## Output (concise)

## Task Summary
...

## Task Type
...

## Affected Areas
- ...

## Selected Agents
- ...

## Execution Plan
1. ...
2. ...

## Key Changes
- ...

## Risks
- ...

## Validation
- ...

## Out of Scope
- ...

---

## Rules

- Keep output concise (max ~300 words unless complex)
- Do NOT generate code
- Do NOT expand scope
- Prefer minimal safe solution