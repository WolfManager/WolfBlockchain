# Agents Safety Model

## Principles

- Agents cannot directly mutate critical blockchain state.
- Every action is evaluated by policy engine and gateway.
- Provider integration is isolated via adapters.
- Memory store is separated from chain state.

## Enforcement path

1. Request enters orchestrator.
2. Policy engine evaluates action + context.
3. Safe gateway enforces mutation restrictions.
4. Audit event is emitted for decision/outcome.

## Default rule

`SubmitTransaction` requires explicit authorization marker; deny by default otherwise.
