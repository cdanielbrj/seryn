# ADR-008: Frontend Application Architecture

## Status

Accepted.

## Context

The initial `core`, `features`, and `global` structure separated broad concerns but did not make dependency direction or the role of individual files explicit. Application bootstrap, persistent layout, route composition, HTTP infrastructure, reusable React contracts, and product-specific behavior could still be grouped under ambiguous directories.

The application contract must be as deliberate as the theme structure without forcing every reusable concern into a generic shared directory or organizing the code primarily around pages.

## Decision

Organize `src/web/app` into explicit architectural responsibilities:

- `bootstrap` composes session startup and application routing;
- `layout` implements the persistent authenticated application structure and navigation;
- `components/primitives` contains small domain-neutral React contracts;
- `components/patterns` contains reusable compositions of primitives;
- `infrastructure` contains technical mechanisms that do not know product concepts, initially the HTTP client;
- `modules` contains product capabilities, each divided only into the `api`, `model`, `lib`, `components`, and `routes` directories it actually needs;
- `formatters` contains proven domain-neutral representation functions with multiple module consumers.

Module `api` files own endpoint paths and transport-facing operations. Routes coordinate loading, mutations, navigation, and module components without calling the HTTP client directly. Module models define product concepts, while module libraries contain pure helpers specific to that capability. Module components may depend on their own model and on root component contracts, but root components must never depend on a product module.

Cross-module imports are avoided. A capability required by application composition may expose a deliberate API, such as session operations from the authentication module. Code is promoted out of a module only after it has independent semantics and more than one real consumer; currency formatting is the initial example.

Frontend unit tests mirror the module structure under `tests/web/unit/modules` while remaining outside the frontend package and using its single dependency installation.

## Dependency Direction

The intended direction is:

```text
bootstrap -> layout and module routes
module routes -> module api, model, lib, and components
module components -> module model/lib, root components, and formatters
module api -> infrastructure/http
root components -> other root components and component-local helpers
```

Concrete theme CSS remains outside `app`. React files expose semantic structure and behavior but do not import a theme implementation.

## Consequences

- Directory names describe architectural roles instead of reuse frequency.
- Endpoint paths and HTTP transport details no longer leak into route components.
- Product-specific code remains local to its module instead of accumulating in a global directory.
- Persistent application layout is distinct from route content and reusable component patterns.
- New modules may start small and add internal directories only when concrete code requires them.
- Promoting code to a root-level contract requires evidence of domain neutrality and actual reuse.
