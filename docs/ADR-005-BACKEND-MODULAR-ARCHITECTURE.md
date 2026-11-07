# ADR-005 — Backend modular architecture

Status: accepted and implemented for the current backend modules.

## Context

Seryn remains a single-process ASP.NET Core application backed by SQLite. The initial implementation grouped code mostly by technical role and allowed application, HTTP, persistence, and financial calculation concerns to accumulate in a few services. This made feature ownership unclear and coupled the recurrence calculator to EF Core persistence entities.

The application needs clearer boundaries before import/export, backup, and additional record behavior are implemented. It does not need independent deployment units or the operational complexity of microservices.

## Decision

The backend uses a modular monolith organized primarily by product capability. Modules are registered and mapped explicitly from the composition root. A module may contain these internal areas when they add value:

- `Contracts` contains the module's HTTP request and response shapes.
- `Application` coordinates use cases, validation, persistence, and result mapping.
- `Domain` contains business concepts and pure calculations.
- The module root contains endpoint mapping and dependency registration.

New behavior should be grouped as a vertical slice inside its owning module when a module grows beyond a small cohesive service. The slice keeps its endpoint, request, validation, application operation, and response close together. This is an organization technique, not an adoption of CQRS infrastructure.

The solution continues to use one server project and one test project. Namespace and dependency rules provide the current module boundaries. Additional projects require a concrete isolation need.

## Dependency rules

Dependencies point inward from HTTP and persistence toward application and domain behavior:

```text
HTTP endpoint -> application use case -> domain rules
                         |
                         +-> EF Core persistence
```

- Domain calculations do not depend on ASP.NET Core, EF Core, JSON contracts, or SQLite.
- HTTP endpoints bind requests, invoke one application operation, and translate its result to HTTP.
- Application code owns orchestration and authoritative backend validation.
- EF Core remains the direct persistence API. Generic repositories, a custom unit of work, and interfaces without a second implementation are not introduced.
- Cross-module code goes into `Shared` only after it represents a stable concept genuinely shared by multiple modules.
- Import adapters must invoke the same record application operations and validation used by the HTTP API.

## Initial implementation

`Auth`, `Classifications`, `Records`, and `Projections` are organized as explicit modules. Each module owns dependency registration, endpoint mapping, contracts, and its application or domain behavior.

Record direction and recurrence are domain concepts under `Modules/Records/Domain`. The existing EF Core record entity remains a persistence model and uses those domain types.

The recurrence calculator now accepts immutable `ProjectionRecord` values and returns virtual occurrences. It has no EF Core or HTTP dependency. The projection application service performs an EF Core query that maps persisted records into this pure input model before invoking the calculator.

Module classes own dependency registration and endpoint mapping:

```csharp
builder.Services.AddRecordsModule();
builder.Services.AddProjectionsModule();

api.MapRecordsModule();
api.MapProjectionsModule();
```

Record CRUD is split into one handler per use case. Types, payment methods, and categories have separate classification handlers while sharing only their common normalized-name persistence behavior.

Application hosting concerns live under `Infrastructure`: database and data-protection setup, security configuration, database initialization, and the HTTP pipeline. `Program.cs` remains the explicit composition root. EF Core mappings are grouped under `Persistence/Configurations` by persisted aggregate.

## Deliberate exclusions

This decision does not introduce MediatR, a message bus, generic repositories, AutoMapper, domain events, assembly scanning, or separate projects per layer or module. Commands and queries may be used as plain application types where they improve a slice, without requiring a CQRS framework.

The current product behavior for payment methods, supported recurrence frequencies, total-value rounding, and the seven-month contextual chart remains unchanged. Those are product decisions independent from this structural refactoring.

## Consequences

Financial calculations can be tested without starting the web application or creating a database. Persistence queries remain visible and optimized for each use case. Feature code gains a clear owner while Seryn keeps its single container, process, database, and deployment model.

Future modules should follow the same boundaries. Existing behavior and API contracts remain stable while internal slices evolve.
