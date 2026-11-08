# Backend architecture

The backend is a single ASP.NET Core 10 project organized as a modular monolith. `Program.cs` is the composition root; modules own their endpoint mapping, dependency registration, contracts, and application or domain behavior.

## Boundaries

- `Modules/Auth` owns provisioning, credentials, sessions, and authentication endpoints.
- `Modules/Classifications` owns types, categories, and payment methods.
- `Modules/Records` owns the canonical financial record and CRUD use cases.
- `Modules/Projections` owns pure recurrence expansion and derived dashboard reads.
- `Infrastructure` owns hosting, security, Data Protection, and database initialization mechanisms.
- `Persistence` owns EF Core context, entity configuration, and migrations.

HTTP endpoints bind contracts, invoke application behavior, and translate results. Financial rules belong in application or pure domain code, never in endpoint handlers or persistence configuration. EF Core remains the direct persistence API; do not introduce generic repositories or abstraction layers without a concrete second implementation.

New behavior stays inside its owning module and may use a vertical slice when that keeps endpoint, validation, operation, and response cohesive. Cross-module code becomes shared only after representing a stable concept with multiple real consumers.

See [ADR-005](../../docs/architecture/adr/ADR-005-BACKEND-MODULAR-ARCHITECTURE.md) and the [product specification](../../docs/product/SERYN_SPEC.md).
