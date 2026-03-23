# Testing Strategy

## Unit focus

- deterministic block/tx validation
- state transition behavior
- mempool admission and deduplication
- fee/reward/supply invariants
- wallet signing boundaries
- agent policy deny/allow behavior

## Integration focus

- Core-Storage boundary
- Core-Consensus boundary
- Networking input validation before Core
- Wallet-Api interaction boundaries
- Agents-Policy decision boundary

## CI gates

- restore
- build with analyzer/warnings policy
- formatting verification
- test + coverage artifact publishing
