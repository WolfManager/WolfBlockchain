---
applyTo: "src/**"
---

# Security instructions

- Never hardcode secrets, keys, seeds, tokens, or admin credentials.
- Validate all external inputs.
- Treat all peer, API, and user input as untrusted.
- Prefer fail-safe behavior.
- Avoid insecure fallbacks in production code.
- Log security-relevant events clearly.
- Keep secret handling isolated from presentation layers.
- Use secure libraries and standard practices for cryptography.
- Do not weaken validation or authorization for convenience.
- Keep sensitive data out of logs and client-visible responses.