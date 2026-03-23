---
applyTo: "src/**/Agents/**,src/**/AI/**,src/**/Orchestration/**,src/**/Providers/**,src/**/Training/**"
---

# AI agents instructions

- Keep provider integrations behind adapters.
- Separate orchestration, memory, prompts, and execution permissions.
- Keep agent actions explicit and reviewable.
- Never allow unrestricted access to critical blockchain functions.
- Track latency, failures, retries, fallback behavior, and provider health.
- Version prompts and policies.
- Keep training pipelines separate from runtime inference paths.
- Keep AI logic isolated from consensus-critical code.
- Define explicit permissions for agent actions.
- Favor safe and auditable behavior over autonomy.