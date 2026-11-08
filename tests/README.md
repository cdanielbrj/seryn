# Tests

All automated test source lives outside production packages under `tests`.

## Structure

```text
tests/
├── server/        backend integration and pure domain tests
└── web/
    ├── unit/      Vitest tests mirroring frontend modules
    ├── e2e/       Playwright browser flows
    ├── setup/     shared test-environment setup
    └── artifacts/ generated reports, screenshots, traces, and results
```

`tests/server` uses the single `Seryn.Tests.csproj`. Backend integration tests use temporary real SQLite databases and migrations. Pure financial calculations remain testable without HTTP or persistence.

`tests/web/unit/modules` mirrors `src/web/app/modules` so product capabilities have an obvious test owner. Frontend tests use the dependencies and configuration owned by `src/web`; they do not maintain another `package.json`, lockfile, or `node_modules` directory.

`tests/web/artifacts` is generated and ignored. Do not commit reports, screenshots, traces, audit output, or previous-run state.

## Isolation

Never run destructive or end-to-end tests against the shared Compose project `seryn` or its `seryn_seryn-data` volume. Browser suites must use a separate Compose project, explicit test credentials, and a disposable volume.

Commands, required checks, and browser setup are documented in [development testing](../docs/development/testing.md). Validation evidence for completed milestones lives under [`docs/validation`](../docs/validation/README.md).
