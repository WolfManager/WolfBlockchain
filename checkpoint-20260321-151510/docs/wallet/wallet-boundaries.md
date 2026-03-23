# Wallet Security Boundaries

- Key creation and signing are isolated in wallet module.
- Private key material is never exposed to API contracts.
- Signing is adapter-based (`ISigner`) for future HSM/external signer support.
- Account and key retrieval are explicit via `IKeyStore`.
- Unsupported algorithms fail explicitly.
