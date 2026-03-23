# Protocol Overview

## Scope

Protocol defines explicit and versioned models for:

- transaction header/body/signature
- block header/body/transaction list
- protocol message header/body
- protocol error codes

## Versioning

- `ProtocolVersion` is attached to all critical envelopes.
- `ProtocolVersions.V1` is current baseline.
- Unsupported versions must fail validation explicitly.

## Determinism

Protocol objects are data contracts only; no hidden side effects.
