Implement the requested task using a controlled multi-agent workflow.

---

## Step 1 — Act as Manager

Summarize the task in one sentence.

Identify affected areas:
- backend
- frontend
- database
- auth
- integration

---

## Step 2 — Select Agents (strict)

Call ONLY the necessary agents:

- Backend Agent → if backend/API/domain/db/auth/integration is involved
- Frontend Agent → if UI/flow/components/state/API usage is involved
- QA-Reviewer Agent → for validation and final check
- Architect Agent → ONLY if:
  - cross-layer impact
  - schema change
  - external integration
  - auth change
  - state/flow change
  - breaking change risk

If not clearly needed → DO NOT call

---

## Step 3 — Implementation Rules (strict)

- implement ONLY what is requested
- DO NOT refactor unrelated code
- DO NOT redesign architecture
- DO NOT introduce new patterns unless required
- DO NOT change backend contracts unless explicitly required
- DO NOT add extra improvements outside the task
- keep changes minimal and scoped
- preserve existing conventions
- if unsure → choose safest minimal solution

---

## Step 4 — Execution

- Backend and Frontend can run in parallel if independent
- Apply changes step by step
- Avoid touching unrelated files

---

## Step 5 — Validation

Use QA-Reviewer Agent to check:
- correctness
- regressions
- missing validation
- permission issues
- overengineering

---

## Output (concise)

## Task
...

## Affected Areas
- ...

## Files Changed
- ...

## Changes Made
- ...

## Risks
- ...

## Validation
- Checked:
- Remaining Risks:
- Follow-up Needed:

## Notes
- assumptions
- limitations

---

## Rules

- Keep output concise
- Do NOT expand scope
- Do NOT silently change behavior