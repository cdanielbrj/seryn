# ADR-006: Frontend Project Structure

## Status

Accepted.

## Context

The frontend package previously mixed application code, static files, unit tests, end-to-end tests, and generated test output below `src/web`. The repeated `src/web/src` path also obscured the role of the directories.

## Decision

Keep the React/Vite package and its single dependency installation in `src/web`. Store application code in `src/web/app`, theme assets in `src/web/themes`, and files copied without transformation in `src/web/static`.

Organize application code by responsibility: `core` owns composition and routing, `features` owns product capabilities, and `global` contains deliberately reusable API, UI, and utility code. Feature-specific models and helpers stay with their feature instead of becoming global code prematurely.

Store all frontend test source in `tests/web`, divided into `unit`, `e2e`, and shared `setup` directories. Generated results, screenshots, traces, and reports belong to the ignored `tests/web/artifacts` directory. Tests use the dependencies and tool configuration owned by `src/web`; they do not maintain a second package or `node_modules` directory.

The visual contract and theme implementation are refined by [ADR-007](ADR-007-FRONTEND-COMPONENT-AND-THEME-ARCHITECTURE.md).

## Consequences

- Product code, visual identity, immutable static assets, test source, and generated output have explicit boundaries.
- Vite, Vitest, Playwright, TypeScript, ESLint, Docker, and shadcn-compatible aliases must point across those boundaries.
- Frontend tests remain independently organized while dependency versions stay centralized in one package lock.
