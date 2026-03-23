---
applyTo: "src/**/Blockchain/**,src/**/Consensus/**,src/**/Networking/**,src/**/Mempool/**,src/**/State/**"
---

# Blockchain-specific instructions

- Keep protocol code deterministic.
- Keep validation separate from execution.
- Do not mix networking logic with consensus rules.
- Make block, transaction, and state transition logic explicit.
- Use versioned message and block structures.
- Centralize protocol constants.
- Prefer correctness, auditability, and testability over clever optimization.
- Add tests for all critical validation paths.
- Keep consensus-critical paths small and easy to review.
- Avoid hidden side effects in validation and state transition logic.