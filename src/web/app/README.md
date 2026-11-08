# Frontend application architecture

The React application uses a **Modular Vertical-Slice Frontend Architecture**. Directories describe architectural roles rather than reuse frequency.

## Root responsibilities

- `bootstrap`: session startup and route composition.
- `layout`: persistent authenticated structure and navigation.
- `components/primitives`: small domain-neutral React contracts.
- `components/patterns`: reusable compositions of primitives.
- `formatters`: proven domain-neutral representation functions with multiple consumers.
- `infrastructure`: technical mechanisms that do not know product concepts.
- `modules`: product capabilities organized into only the internal areas they need.

## Module responsibilities

A module may contain:

- `api`: endpoint paths and transport-facing operations;
- `model`: product concepts and state contracts;
- `lib`: pure helpers specific to the module;
- `components`: React components that know the module domain;
- `routes`: loading, mutations, navigation, and final composition.

## Dependency rules

```text
bootstrap -> layout and module routes
module routes -> module api, model, lib, and components
module components -> module model/lib, root components, and formatters
module api -> infrastructure/http
root components -> other root components and component-local helpers
```

Routes do not call the HTTP client directly. Root components never import product modules. Avoid cross-module imports unless a capability intentionally exposes an application-level API. Promote code out of a module only after it has independent semantics and multiple real consumers.

React files define semantic structure and behavior but never import a concrete theme. See [ADR-008](../../../docs/architecture/adr/ADR-008-FRONTEND-APPLICATION-ARCHITECTURE.md) and the [theme guide](../themes/README.md).
