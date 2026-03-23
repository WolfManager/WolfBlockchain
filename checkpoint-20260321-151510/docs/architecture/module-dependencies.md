# WolfBlockchain Dependency Rules

## Project map

- `src/WolfBlockchain.Protocol/WolfBlockchain.Protocol.csproj`
- `src/WolfBlockchain.Core/WolfBlockchain.Core.csproj`
- `src/WolfBlockchain.Storage/WolfBlockchain.Storage.csproj`
- `src/WolfBlockchain.Consensus/WolfBlockchain.Consensus.csproj`
- `src/WolfBlockchain.Networking/WolfBlockchain.Networking.csproj`
- `src/WolfBlockchain.Wallet/WolfBlockchain.Wallet.csproj`
- `src/WolfBlockchain.Api/WolfBlockchain.Api.csproj`
- `src/WolfBlockchain.Agents/WolfBlockchain.Agents.csproj`
- `src/WolfBlockchain.Observability/WolfBlockchain.Observability.csproj`

## Allowed compile-time dependencies

| From | Allowed references |
|---|---|
| `Protocol` | none |
| `Observability` | none |
| `Core` | `Protocol`, `Observability` |
| `Storage` | `Protocol`, `Core`, `Observability` |
| `Consensus` | `Protocol`, `Core`, `Observability` |
| `Networking` | `Protocol`, `Core`, `Observability` |
| `Wallet` | `Protocol`, `Core`, `Observability` |
| `Agents` | `Protocol`, `Core`, `Wallet`, `Observability` |
| `Api` | `Protocol`, `Core`, `Wallet`, `Agents`, `Observability` |

## Forbidden dependencies

- `Protocol` must not depend on any project.
- `Observability` must not depend on any project.
- `Core` must not depend on `Api`, `Agents`, `Networking`, `Consensus`, `Storage`, `Wallet`.
- `Consensus` must not depend on `Api`, `Networking`, `Storage`, `Wallet`, `Agents`.
- `Networking` must not depend on `Consensus`, `Api`, `Storage`, `Wallet`, `Agents`.
- `Storage` must not depend on `Api`, `Networking`, `Consensus`, `Wallet`, `Agents`.
- `Wallet` must not depend on `Api`, `Networking`, `Consensus`, `Storage`, `Agents`.
- `Agents` must not depend on `Api`, `Networking`, `Consensus`, `Storage`.
- `Api` must not be referenced by any `src` project.

## Boundary constraints

- Validation and state transitions stay in `Core`; no transport or web concerns.
- `Networking` validates untrusted external input before passing data to `Core`.
- `Consensus` consumes `Core` contracts and never embeds HTTP or storage details.
- `Agents` cannot directly mutate critical chain state without `Core` validation interfaces.
- Security and audit events flow through `Observability` contracts.

## Note

Dependency definition was requested twice in the task list; this single section covers both requests explicitly.
