# Core Validation and State Transition Flow

1. Receive `BlockEnvelope`.
2. Validate block structure and transaction set (`IBlockValidator`).
3. Validate each transaction (`ITransactionValidator`).
4. Execute deterministic state transition checks (`IStateTransitionExecutor`).
5. Apply state update through explicit updater (`IStateUpdater`).
6. Return explicit `ValidationResult`.

## Separation

- Validation is isolated from execution.
- Execution is isolated from state mutation.
- Networking and API are excluded from core path.
