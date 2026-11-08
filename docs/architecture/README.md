# Architecture

Seryn is a self-hosted modular monolith: one ASP.NET Core process serves the API and compiled React application, with SQLite persisted under `/data`.

The current implementation is governed by the accepted architecture decision records below. ADR-001 is embedded in the approved architecture section of the [product specification](../product/SERYN_SPEC.md#8-approved-architecture-adr-001).

## Decision index

| ADR | Decision | Status |
| --- | --- | --- |
| [ADR-002](adr/ADR-002-PHASE-1.md) | Phase 1 foundation, security, persistence, and distribution | Implemented |
| [ADR-003](adr/ADR-003-PHASE-2-RECORDS.md) | Canonical record storage and CRUD | Accepted |
| [ADR-004](adr/ADR-004-DASHBOARD-PROJECTIONS.md) | Derived dashboard projections | Accepted |
| [ADR-005](adr/ADR-005-BACKEND-MODULAR-ARCHITECTURE.md) | Backend modular monolith | Accepted |
| [ADR-006](adr/ADR-006-FRONTEND-PROJECT-STRUCTURE.md) | Frontend package and test boundaries | Accepted |
| [ADR-007](adr/ADR-007-FRONTEND-COMPONENT-AND-THEME-ARCHITECTURE.md) | Component and theme contract | Accepted |
| [ADR-008](adr/ADR-008-FRONTEND-APPLICATION-ARCHITECTURE.md) | Modular vertical-slice frontend | Accepted |

## When to add an ADR

Create an ADR when a decision changes a durable boundary, dependency direction, data model, security model, deployment topology, or cross-cutting engineering convention. Update current documentation instead when behavior or commands change without introducing a new architectural decision.

ADRs use the next sequential number and contain at least status, context, decision, and consequences. Accepted ADRs are immutable in intent; superseding decisions should link to the earlier ADR and explicitly change its status.
