# Initial Threat Model

## Peer attacks

- malformed messages
- spam/flood attempts
- invalid protocol versions

Mitigation: strict external message validation, early drop in networking router.

## Transaction attacks

- invalid transaction structure/signature
- duplicate transaction replay in mempool

Mitigation: deterministic validators + mempool deduplication.

## Consensus risks

- invalid proposal height/round
- malformed consensus messages

Mitigation: plugin validation + explicit message handler checks.

## Key exposure risks

- private key leakage via logs/API

Mitigation: key isolation in wallet module; no secret fields in API responses.

## Admin misuse

- unauthorized admin endpoint invocation

Mitigation: explicit RBAC check (`admin` role) + audit logging.

## Agent abuse

- direct critical state mutation by autonomous actions

Mitigation: policy engine + safe gateway deny-by-default for unsafe submit actions.

## Storage corruption

- inconsistent canonical state and block data

Mitigation: explicit storage interfaces, append-only audit patterns, deterministic tests.
