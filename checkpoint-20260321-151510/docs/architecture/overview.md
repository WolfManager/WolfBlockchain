# WolfBlockchain Architecture Overview

WolfBlockchain uses a modular architecture with strict boundaries:

- `Protocol`: versioned wire and block/tx contracts.
- `Core`: deterministic validation, state transitions, mempool, economics rules.
- `Storage`: canonical block/state stores and audit log.
- `Consensus`: plugin-based consensus orchestration.
- `Networking`: peer sessions, transport, input validation, message routing.
- `Wallet`: key management and signing adapters.
- `Api`: public and admin endpoints, request validation, consistent responses.
- `Agents`: orchestration, providers, policies, and controlled action gateway.
- `Observability`: structured audit, metrics, health, and trace hooks.

Design goals: security-first, deterministic core paths, explicit contracts, and long-term scalability.
