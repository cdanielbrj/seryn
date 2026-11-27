# Lumen architecture

## Goals

Lumen provides a common vocabulary and dependency direction for AI-assisted applications. It standardizes coordination and connection boundaries while leaving product behavior, persistence, authentication mechanisms, and user experience under host ownership.

Lumen is not an autonomous agent runtime, a mandatory remote service, a model gateway business, or a replacement for the host's application layer.

## Boundaries

### Core

Core owns provider-neutral conversation and tool-call coordination:

- bounded message context;
- provider requests and normalized responses;
- tool definitions and execution results;
- round, call, size, concurrency, and timeout limits;
- host policy injection;
- safe failure translation.

Core depends on contracts. It must not depend directly on a concrete provider, database, HTTP authentication handler, or product module.

### Optical Link

An Optical Link adapts one model provider to Core. It owns provider-specific authentication, request and response mapping, a default model preference, validation of conversation-level model overrides, transport controls, and provider failures. It does not own the conversation, product persona, or host tools.

The host may let a conversation override the connection's default model. That override is conversation state: it must remain stable across tool rounds and must not rewrite the connection. Provider model discovery is an optional adapter capability rather than a Core requirement.

### Application Link

An Application Link authenticates external consumers and maps their granted capabilities to host operations. It owns credential issuance or acceptance, revocation, expiration, and consumer identity. Interactive human authentication is a separate host concern.

Seryn Link is the first Application Link. Other hosts should use a host-specific name rather than treating `Seryn Link` as a generic protocol.

### Host adapters

The host supplies:

- tools backed by its existing application services and validation;
- persona and product instructions;
- authorization policies and capability mapping;
- persistence adapters for link configuration;
- user interface and deployment integration.

Tools never bypass the host application layer to write directly to persistence.
Core receives persona instructions through a host contract and defines no shared voice, identity, or judgment style. Each host owns and tests its persona independently.

## Dependency direction

```text
host modules ──▶ host tool adapter ──▶ Core contracts
Optical Link ────────────────────────▶ Core provider contracts
Application Link ───────────────────▶ host authorization contracts
composition root ──▶ all concrete implementations
```

Core must not import Optical Link, Application Link, or host modules. Concrete adapters may import Core contracts.

## Seryn reference implementation

Seryn keeps Lumen inside its single ASP.NET Core process and React application:

```text
src/server/Lumen/
├── Core/
├── OpticalLink/
├── Persona/
├── SerynLink/
└── Tools/

src/web/app/modules/lumen/
├── api/
├── chat/
├── optical-link/
└── seryn-link/
```

The Seryn tool catalog depends on Seryn modules and is not portable Core code. EF Core entities, ASP.NET endpoint mapping, Brazilian Portuguese public contracts, and React routes are also host implementation details.

Seryn exposes the current Lumen API under `/api/v1/lumen`:

- `/chat` for conversation coordination;
- `/optical-links` for provider connections;
- `/seryn-links` for external API access.

## Extraction rule

Do not extract a shared package merely because two files look similar. Extract after at least two hosts demonstrate the same semantics and dependency boundary. Prefer source-level adaptation until that evidence exists.
