# AGENTS.md — Seryn

This repository implements **Seryn**, a self-hosted planner for expected income and expenses. **Approved stack: React/TypeScript/Vite on the frontend, ASP.NET Core 10/C# on the backend, and EF Core 10 with SQLite for persistence.** **Read `docs/product/SERYN_SPEC.md` in full before modifying the project.** It defines the product, financial rules, approved UI, and acceptance criteria and takes precedence over generic assumptions about financial applications. Current technical decisions live under `docs/architecture/`.

## Invariants

- Use one record model for every expected income or expense; the stored amount is always the per-occurrence value in integer cents.
- Calculate recurrences in queries. Never generate future installments or entries in SQLite.
- Totals, series end dates, projections, and results are derived values, not persisted sources of truth.
- All values are expected. Do not introduce paid, settled, pending, or bank-account concepts.
- Type and category are editable classifications and never determine financial behavior.
- CSV/Excel import and export must preserve the original record and support round trips without silent duplication.
- Run the self-hosted application in one Docker container and ASP.NET Core/Kestrel process, with SQLite persisted under `/data`; Node exists only during the React build.
- Keep a modular monolith. Do not introduce microservices, auxiliary services, or mandatory third-party authentication. Use `/api/v1` and OpenAPI for future integrations.
- UI, imports, and future APIs must use the same application service and validation. Never write directly to the database around those rules.
- Require a secure single-user first access before exposing real data. Protect routes, sessions, backups, and exports.
- Preserve the desktop dashboard sections from `docs/design/prototypes/seryn_mock_v2.html`. Mobile uses its own composition with bottom navigation. Support light and dark modes.
- The prototype contains fictitious data, not valid financial algorithms. Never copy its static calculations.
- A projection is not a bank balance. The chart uses the accumulated result within its displayed period and starts at zero.

## Working in the repository

1. Read `docs/product/SERYN_SPEC.md` and inspect `docs/design/`.
2. Preserve .NET 10 LTS, EF Core 10, the modular monolith, and the pure C# recurrence layer. Implement small, testable increments.
3. Do not add functionality outside v0.1 without a request, including Aira connectors, third-party tokens, bank accounts, or investment modules.
4. Run builds, lint, tests, and canonical numeric examples before declaring completion.
5. Document implementation decisions not already covered by the product specification.
6. End each delivery with what works, how it was tested, and what remains.
7. Write all repository content in English, including READMEs, documentation, ADRs, release descriptions, and workflows. Product UI and public contracts remain in Brazilian Portuguese as required by the specification.

## Agent workflows

- Versioned repository skills live under `.agents/skills/`. Use the matching skill when a task fits its description.
- Keep only always-applicable rules in `AGENTS.md`. Detailed procedures belong in skills; canonical knowledge belongs in `docs/` and local READMEs.

## Shared local development instance

- Keep the Compose project `seryn` running at `http://localhost:8080` for shared validation.
- Update it with `docker compose -p seryn up --build -d --wait`. Container recreation is allowed; preserve the `seryn_seryn-data` volume, credentials, and existing data.
- Do not stop this instance when finishing a task, and never run `down -v` against it. Destructive and end-to-end tests must use a separate Compose project with a disposable volume.
