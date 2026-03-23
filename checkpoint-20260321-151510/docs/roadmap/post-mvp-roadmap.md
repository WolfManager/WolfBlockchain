# Post-MVP Technical Roadmap

## Next priorities

1. Persisted storage backends (RocksDB/PostgreSQL adapters) with migration strategy.
2. Multi-node networking hardening (timeouts, retries, peer scoring, rate limits).
3. Consensus evolution from single-node plugin to BFT-capable plugin set.
4. Full API host project with auth middleware and versioned OpenAPI docs.
5. Wallet hardening for external signers/HSM integration.
6. Expanded deterministic and property-based tests for core/economics.

## Defer after foundation

- complex fee market
- advanced tokenomics/governance modules
- high-throughput optimization
- autonomous agent workflows with broad privileges

## Risks

- accidental cross-module coupling
- non-deterministic core changes
- hidden privileged controls
- weak operational observability

## Recommended execution order

1. storage durability
2. consensus/network hardening
3. API and admin auth hardening
4. wallet + key lifecycle hardening
5. agent policy maturity
6. performance/scalability tuning
