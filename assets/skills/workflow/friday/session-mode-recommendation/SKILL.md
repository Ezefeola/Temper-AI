---
name: friday-session-mode-recommendation
description: >
  Session-mode recommendation policy for FRIDAY. Load when FRIDAY needs to
  recommend clean session versus continue here after a completed specialist step
  and an approved meaningful continuation.
---

# FRIDAY Session Mode Recommendation Policy

## Purpose

This skill defines when FRIDAY should recommend `clean session` versus `continue here`.

When loaded, it is authoritative for:

- Whether a session-mode recommendation should be offered at all.
- The default recommendation bias.
- State-based counters and continuation signals.
- Practical thresholds and hard exceptions.
- The required recommendation output format.

FRIDAY recommends; the user decides.

## When To Load

Load this skill only after a specialist step is complete and FRIDAY is evaluating whether the next meaningful work should continue in the same conversation or from saved state.

Do not load it for:

- Startup classification.
- Generic approval requests.
- Active analyst or architect loop questions that have not reached completion.
- Turns where no meaningful continuation is available yet.

## Offer Gate

Offer a session-mode recommendation only when at least one of these is true:

- There is a meaningful next specialist step that is already approved or is the exact next approved continuation.
- There is substantial continuation still expected in the same workflow and session choice affects quality.

Do not offer a session-mode recommendation when any of these are true:

- FRIDAY is only waiting for output approval, rejection, or change requests.
- The workflow is complete and no next specialist step is being prepared.
- The user must answer an analyst or architect loop question before any new specialist step can happen.
- FRIDAY is blocked on missing domain input, repair approval, reset approval, or direction change.
- The next action is only a short direct FRIDAY answer with no specialist continuation.

## Default Bias

Default to `continue here`.

Use `clean session` only when the continuation signals clearly show that the next step would benefit from narrower, fresher context.

## Persisted Metrics Source

Use persisted `session_metrics` from `.temper/friday-state.json` as the primary source for recommendation decisions.

Expected fields:

- `specialist_runs_total`
- `specialist_runs_current_thread`
- `distinct_specialists`
- `distinct_specialist_count`
- `cycle_rounds_total`
- `recovery_attempts`
- `prompt_failure_retries`
- `long_outputs_count`
- `large_summaries_count`
- `last_output_class`
- `next_action_type`
- `scope_confusion_signals`
- `mixed_scope_signal`
- `risk_score`

If `session_metrics` is missing because the state file predates this policy, default missing counters to `0`, default `mixed_scope_signal` to `false`, default `last_output_class` to `none`, default `next_action_type` from current checkpoint intent, recompute `risk_score`, and keep the default bias to `continue here`.

## Practical Thresholds

Normalize `next_action_type` before evaluating thresholds:

- Continuation types that may justify an offer: `approved-next-specialist`, `approved-exact-continuation`.
- Types that suppress an offer: `output-approval`, `analyst-cycle-input`, `architect-cycle-input`, `recovery-approval`, `repair-or-reset`, `blocked-input`, `direct-answer`, `change-direction`, `none`.

Compute `risk_score` from persisted metrics using this additive model:

- `+1` when `specialist_runs_current_thread >= 2`
- `+1` when `distinct_specialist_count >= 2`
- `+1` when `cycle_rounds_total >= 3`
- `+1` when `recovery_attempts >= 1`
- `+1` when `prompt_failure_retries >= 1`
- `+1` when `long_outputs_count >= 2`
- `+1` when `large_summaries_count >= 1`
- `+1` when `last_output_class` is `failure`, `partial-output`, `unclear-output`, or `recovery-report`
- `+1` when `scope_confusion_signals >= 1`
- `+2` when `mixed_scope_signal` is `true`

Persist the computed total back to `session_metrics.risk_score` when FRIDAY writes state.

Recommend `continue here` when all of these are true:

- `next_action_type` is `approved-next-specialist` or `approved-exact-continuation`.
- `risk_score <= 2`.
- `specialist_runs_current_thread <= 1`.
- `long_outputs_count <= 1`.
- `large_summaries_count = 0`.
- `recovery_attempts = 0`.
- `prompt_failure_retries = 0`.
- `scope_confusion_signals = 0`.
- `mixed_scope_signal = false`.

Recommend `clean session` when any of these threshold groups is true:

- `risk_score >= 4`
- `specialist_runs_current_thread >= 3`
- `distinct_specialist_count >= 3`
- `cycle_rounds_total >= 4`
- `recovery_attempts + prompt_failure_retries >= 2`
- `long_outputs_count + large_summaries_count >= 3`

## Hard Exceptions

Recommend `continue here` even if some noise exists when any of these are true:

- `next_action_type` is `approved-exact-continuation`, `risk_score <= 3`, and both `mixed_scope_signal = false` and `scope_confusion_signals = 0`.
- The next step is immediate, narrow, and tightly coupled to the just-completed specialist output, with `specialist_runs_current_thread <= 2` and no recovery history.
- FRIDAY only needs to perform a short approved continuation and the only load signal is `long_outputs_count = 1` or `large_summaries_count = 1`.

Recommend `clean session` immediately when any of these are true:

- `mixed_scope_signal = true`.
- `scope_confusion_signals >= 2`.
- `risk_score >= 6`.
- `next_action_type = approved-next-specialist` and the next specialist shifts focus after recovery-heavy or report-heavy context: `recovery_attempts >= 1` plus `long_outputs_count + large_summaries_count >= 2`.

## Output Format

Use exactly this structure:

```text
Recommendation: clean session | continue here
Reason: [one concise reason tied to current continuation signals]
Choices: Reply "clean" to continue from saved state with focused context, or "continue here" to proceed in this session.
```

Keep the reason operational and short. Do not give a long essay.

## Decision Rule

If the signals are mixed or borderline, prefer `continue here`.

Only recommend `clean session` when the practical thresholds or hard exceptions clearly justify it.

Decision order:

1. Apply the offer gate.
2. Read `session_metrics` and recompute `risk_score` if needed.
3. Apply `continue here` hard exceptions first when continuation is immediate and tightly scoped.
4. Apply `clean session` hard exceptions next.
5. If no hard exception fired, use the practical thresholds.
6. If still mixed or borderline, recommend `continue here`.
