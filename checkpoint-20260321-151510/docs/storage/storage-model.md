# Storage Model

## Canonical stores

- `IBlockStore`: canonical block persistence by hash.
- `IStateStore`: state root by height.
- `IChainReadRepository`: current chain height and transaction presence.

## Audit store

- `IAuditLogStore`: append-only events for security/economic/admin actions.
- explicit `AuditLogEntry` with timestamp, category, event type, and actor.

## Current baseline

In-memory implementations are used for deterministic foundation testing.
