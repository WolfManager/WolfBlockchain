---
applyTo: "src/**/Token/**,src/**/Economics/**,src/**/Fees/**,src/**/Rewards/**"
---

# Tokenomics instructions

- Keep fee logic deterministic and testable.
- Make issuance, burning, supply, and reward rules explicit.
- Do not hide privileged controls.
- Keep accounting rules auditable.
- Separate economic logic from UI and transport layers.
- Add tests for all supply-changing and fee-related logic.
- Avoid implicit monetary behavior.
- Document all token and fee assumptions clearly.
- Keep reward distribution rules transparent.
- Favor simple and explainable tokenomics over unnecessary complexity.