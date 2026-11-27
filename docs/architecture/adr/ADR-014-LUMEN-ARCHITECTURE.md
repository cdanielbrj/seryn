# ADR-014 — Lumen integration architecture

Status: accepted and experimental.

## Context

Seryn needs a conversational interface that can use its existing application behavior while delegating language-model inference to a user-configured provider. It also already exposes scoped API access for external clients under ADR-012. Both directions share security and tool-contract concerns, but they have different credentials, trust boundaries, and lifecycles.

The same pattern may be useful in other applications. Extracting a library before a second host exists, however, would turn assumptions from the first implementation into premature compatibility commitments.

## Decision

Adopt **Lumen** as the common architecture for model-mediated integrations. Its portable specification lives in the repository-root [`lumen/`](../../../lumen/README.md) directory, while executable code remains adapted to each host application.

Lumen has these boundaries:

- **Core** defines provider-neutral conversations, normalized messages, tool definitions, tool execution, and bounded results.
- **Optical Link** manages outbound connections from a host to external inference providers. Provider credentials are encrypted at rest and never exposed again after creation.
- **Application Link** defines inbound access for external applications and agents. **Seryn Link** is Seryn's Application Link implementation and retains the scoped personal-access-token security model established by ADR-012.
- **Host adapters** expose only explicit tools and execute them through the same application services and validation used by the product UI and API.

Seryn implements these boundaries in `src/server/Lumen` and `src/web/app/modules/lumen`. Interactive administrator authentication remains owned by `Modules/Auth`; `Infrastructure` composes cookie and Seryn Link authentication without making either depend on the other.

The initial conversational tools are read-only. Conversations are ephemeral and bounded, model output is not a financial source of truth, and exact calculations remain host-owned. Public Lumen endpoints use `/api/v1/lumen`, including chat, Optical Link administration, and Seryn Link administration.

The root specification is portable documentation, not a shared runtime package. A reusable package or independent repository will be considered only after at least two real host implementations reveal a stable common contract.

## Consequences

Inbound and outbound integrations share vocabulary and design rules without sharing secrets or authentication flows. Provider-specific behavior remains behind Optical Link adapters, and Seryn's financial rules remain inside its application services.

Moving the pre-release integration endpoints into `/api/v1/lumen` is an intentional contract correction; the former routes are not retained as aliases. ADR-012 remains authoritative for Seryn Link token hashing, scopes, revocation, idempotency, and API compatibility.

Lumen is experimental while Seryn is its only host. Its contracts may be refined from implementation evidence, but changes must update the root specification, host documentation, and the versioned Lumen development skill together.
