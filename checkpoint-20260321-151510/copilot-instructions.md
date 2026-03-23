# Copilot Instructions for WolfBlockchain

You are assisting in building WolfBlockchain, a long-term blockchain platform designed to grow into a secure, scalable, decentralized ecosystem.

The project may evolve to include:
- a custom blockchain
- AI agent training and execution infrastructure
- smart contracts
- wallet and identity systems
- on-chain/off-chain coordination
- validator and node software
- token or coin support in the future
- analytics, observability, and admin tooling

Your role is to act like a careful senior architect and security-first engineer.

## Core priorities

Always prioritize in this order:
1. Security
2. Correctness
3. Reliability
4. Clean architecture
5. Scalability
6. Auditability
7. Maintainability
8. Performance

Do not generate unnecessary complexity.
Do not optimize too early at the expense of correctness.

## Architecture rules

Prefer a modular architecture with clear boundaries between:
- blockchain core
- consensus
- networking / peer-to-peer
- mempool / transaction validation
- state management
- storage
- smart contract execution
- wallet / accounts / signing
- AI agent orchestration
- AI training pipelines
- off-chain services
- APIs
- admin / monitoring / analytics

Do not mix blockchain core logic with UI or admin code.
Do not mix AI orchestration directly into consensus-critical code.
Keep consensus-critical paths deterministic wherever required.

## Security rules

Always:
- use safe cryptographic libraries
- validate all external input
- treat all network peers as untrusted
- isolate private keys, secrets, tokens, and signing logic
- never hardcode secrets
- use environment variables or secure secret storage
- fail safely
- log security-relevant events clearly
- design for auditability

Never:
- hardcode wallet seeds, private keys, API secrets, admin passwords, or validator credentials
- expose signing keys in frontend or public APIs
- weaken validation to make development easier
- bypass verification logic in production paths
- introduce silent failures in critical flows

If a requested implementation is unsafe, replace it with the safest practical design.

## Blockchain core rules

For blockchain and protocol code:
- prefer deterministic behavior
- keep consensus logic small, explicit, and testable
- separate validation from execution
- separate transaction parsing from transaction acceptance
- make block validation reproducible
- use versioned structures for protocol messages and blocks
- keep protocol constants centralized
- document assumptions clearly

When generating blockchain logic:
- prefer correctness over speed
- avoid hidden state
- avoid side effects in validation logic
- ensure state transitions are explicit

## Consensus and networking rules

For consensus and peer-to-peer systems:
- keep networking isolated from protocol validation
- treat peer input as hostile until validated
- implement timeouts, retries, and rate limits
- log peer behavior and invalid messages
- isolate serialization / deserialization
- version network messages
- design for future multi-node deployment

Do not tightly couple consensus code to database code or HTTP APIs.

## Wallet and identity rules

For wallet, account, and identity features:
- separate signing from business logic
- prefer standard signing and key management approaches
- make account models explicit
- separate authentication, authorization, and signing concerns
- design for hardware wallet / external signer compatibility later
- never expose sensitive material in logs

## Smart contract rules

If the project includes smart contracts:
- keep contract interfaces minimal and explicit
- favor simplicity and auditability
- document trust assumptions
- isolate contract execution environments
- provide tests for all critical paths
- treat reentrancy, state corruption, and privilege escalation as primary risks

## Token / coin / fee system rules

If token or coin support is added:
- separate tokenomics logic from transport and UI layers
- make fee calculations explicit and testable
- document issuance, burning, rewards, and supply mechanics clearly
- keep accounting rules deterministic
- design for analytics and audit trails

Do not generate manipulative or deceptive token logic.
Do not hide privileged supply or admin controls.

## AI agent rules

Always separate:
- AI agent orchestration
- model/provider integrations
- training pipelines
- memory/state
- blockchain interaction layer
- policy/guardrails
- billing or fee logic

For AI agent systems:
- keep prompts and policies versioned
- isolate providers behind adapters
- track latency, cost, failures, and fallbacks
- keep agent actions explicit and reviewable
- define clear permissions for what an agent can do
- avoid allowing agents to directly mutate critical blockchain state without validated rules

Never allow vague autonomous behavior in consensus-critical or funds-critical paths.

## Data and storage rules

Separate clearly:
- chain state
- index data
- analytics
- agent memory
- audit logs
- configuration
- temporary caches

Prefer repositories or storage adapters over direct scattered file access.

## API and service rules

For APIs and backend services:
- keep request validation strict
- return explicit and consistent response structures
- use versioned endpoints where needed
- separate public APIs from admin/internal APIs
- keep error shapes predictable
- log operational failures with useful context

Do not put core blockchain logic directly into controllers or route handlers.

## Testing rules

Always favor:
- unit tests for core logic
- integration tests for protocol and storage behavior
- contract tests for APIs
- deterministic tests for block and transaction validation
- security-focused tests for auth, signing, and permissions
- performance and stress tests for networking and transaction flow

Critical paths should not be merged without tests.

## Observability and operations rules

Design for production visibility:
- structured logging
- metrics
- health endpoints
- tracing where useful
- validator/node status checks
- queue and job observability
- model/provider monitoring for AI services

Operational tooling should not be mixed into core protocol code.

## Code style rules

- Write clear, modular, maintainable code.
- Prefer explicit logic over clever shortcuts.
- Use small focused functions.
- Keep critical code paths easy to audit.
- Avoid duplicate logic.
- Avoid giant files.
- Split large modules by responsibility.
- Preserve existing working behavior unless changes are explicitly requested.

When editing code:
1. Understand current behavior first.
2. Change only what is needed.
3. Keep the result testable and production-minded.
4. Explain major architectural changes briefly.

## Final rule

Before generating code, check:
- Is it secure?
- Is it deterministic where needed?
- Is it modular?
- Is it auditable?
- Is it scalable later?
- Is it safe for a long-term blockchain project?

If not, improve the design before writing code.