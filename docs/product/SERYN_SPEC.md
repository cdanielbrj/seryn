# Seryn — Product, architecture, and implementation specification

> **Status:** approved product baseline; consolidated technical stack, analyses, and objectives extensions (revision 4)
> **Reference date:** November 3, 2026  
> **Product language:** Brazilian Portuguese (`pt-BR`)  
> **Audience:** Codex and project contributors  
> **Visual source of truth:** `docs/design/prototypes/seryn_mock_v2.html` and the reference images in the same directory

## 0. Primary implementation instruction

Build **Seryn** as a self-hosted web application, installable as an iPhone PWA, distributed as one Docker container with SQLite persisted in an external volume. Follow this document strictly. The v0.1 visual experience follows the approved v2 prototype. Do not introduce accounting, banking, or payment-tracking behavior that is absent from this specification.

Priority order: projection-engine correctness; data protection, integrity, and portability; mobile UX; visual fidelity; extras. The interface may be refined, but it must never hide an incorrect financial rule.

This document defines product behavior. SQL schema, components, and routes are engineering decisions. A user never defines a schema to create a record: a record created through UI or import becomes a persisted SQLite record directly.

## 1. Identity and purpose

- Name: **Seryn**.
- Technical identifier: `seryn` for packages, services, directories, and images.
- Short description: financial planning through records and recurrence rules.
- Value proposition: show expected income and expenses over a month, semester, or year, grouped by type, category, and direction.
- Visual identity: independent from Warden, Raven, and Aira.
- Product principle: **record once, project for the selected period**.
- Initial currency and locale: BRL and `pt-BR`. v0.1 has no currency conversion.

### What Seryn is not

Seryn is not a bank, wallet, billing system, auditable ledger, investment platform, or payment tracker. It never assumes that an amount was paid, received, settled, overdue, or pending.

All values are user-defined forecasts, not completed transactions. Do not reconcile them with a bank balance, require unexpected expenses, or claim that a result is available cash.

## 2. Non-negotiable principles

1. **One record model for everything.** Salary, subscriptions, sales, installments, expenses, and one-off income differ through attributes, not special classes.
2. **Value is always per occurrence.** SQLite stores the unit amount. A series total is derived.
3. **Recurrence is independent of type and category.** A `Purchase` need not be installment-based, a `Subscription` need not be infinite, and `Income` may occur once. Classifications are editable and never drive financial rules.
4. **Never materialize projected entries.** Do not store one row per future day/week/month. Calculate occurrences only for a requested finite range.
5. **No payment state.** v0.1 must not include fields, screens, indicators, or terminology such as paid, settled, overdue, completed, or pending when they imply execution tracking.
6. **Portable and round-trippable.** CSV and Excel import/export are core features.
7. **Truly responsive.** Mobile uses its own composition instead of scaling down desktop.
8. **One real container.** No PostgreSQL, Redis, separate worker, or external service is required.
9. **Simple and extensible.** Do not pre-build abstractions, entities, or screens for future features.
10. **Never invent data.** Totals, charts, categories, and lists come exclusively from persisted records, except explicitly marked development-only demo data.

## 3. v0.1 functional scope

### Required

- CRUD for income and expense records.
- Description, direction, per-occurrence amount, recurrence start, frequency, finite or unlimited count, type, and category.
- CRUD for editable types and categories, neither tied to financial logic.
- One-time, finite, and unlimited recurrence with deterministic projections.
- Month, semester, and year queries with previous/next navigation.
- Direction, type, category, and description-search filters; grouping by direction, type, and category.
- Expected income, expected expenses, and period result (`income - expenses`).
- Desktop v2 layout: summary, projection chart, category distribution, and projected records.
- Mobile v2 layout: prominent result, income/expenses, a short record list, and fixed bottom navigation.
- Accessible light and dark themes.
- Import/export of original records in CSV, `.xlsx`, and legacy `.xls`, with tested compatibility.
- Import preview/validation and explicit duplicate handling.
- iPhone-installable PWA metadata, icons, and viewport behavior.
- SQLite backup/restore and persistence across container replacement/upgrades.
- Local single-user authentication before financial operations become available, with safe local and reverse-proxy deployment.

### Do not implement now

- Bank accounts, balances, banking synchronization/Open Finance, cards, or invoices.
- Payment confirmation, receipt confirmation, collections, due-date notifications, or reconciliation.
- A dedicated investment/wealth module. An investment-related forecast is an ordinary record.
- AI, OCR, third-party integrations, multiple currencies, or currency conversion.
- Complex multi-user permissions, teams, or public sharing.
- Rules based on type/category text.
- Different installment amounts inside one record; use separate records for different values.
- Extra charts or financial indicators absent from the approved mock.

### Approved extension — analyses

After the original v0.1 dashboard, Seryn adds an authenticated **Análises** area for investigating the composition of projected values. It does not introduce actual transactions, balances, budgets, payment execution, or persisted aggregates.

- Use the same month, semester, and year civil periods and the same recurrence engine as the dashboard.
- Filter by all movements, income, or expenses.
- Segment projected occurrences by root category, payment method, or type.
- Optionally cross the primary dimension with one different dimension.
- Show projected amount, percentage of the filtered volume, original-record count, occurrence count, and the original records composing each segment.
- For a finite recurring record, show its total installment count and how many installments are projected through the end of the selected period. This is recurrence progress, never confirmation that an installment was paid.
- When both directions are selected, percentage basis is total projected volume (`income + expenses`), not the net result. With one direction selected, it is that direction's total.
- Keep income and expense distinguishable and calculate result as projected income minus projected expenses under the active filter.
- Derive every result at query time. Never persist analysis totals or expanded occurrences.
- Use wording such as planned values, projected impact, and composition. Never claim actual spending, available money, affordability, or a bank balance.

The desktop sidebar and mobile primary navigation include **Análises**. The approved v2 dashboard remains unchanged; this area has its own responsive composition.

### Approved extension — objectives

**Objetivos** stores possibilities the user wants to remember, consider, or desire without turning them into financial records or projected commitments.

- Required fields are name, positive estimated value, interest, priority, and an automatic inclusion date.
- Optional fields are an HTTP/HTTPS link, notes, and a desired month.
- Interest is a freely editable classification with exactly `listado`, `considerado`, and `desejado`; it is not a workflow and has no transition rules.
- Priority is a user-defined integer from `-3` through `+3`, defaults to neutral `0`, and is never inferred from value, date, or interest.
- Default ordering is desired before considered before listed, then higher priority, then more recent inclusion. The UI also supports name, value, and inclusion-date ordering.
- Objectives have independent CRUD and never affect records, dashboard totals, analyses, or projections.
- Inclusion date is generated by the application and preserved on edit. Desired date has `YYYY-MM` precision.
- Deleting an objective never deletes or changes a financial record.

Future simulations may refer to an objective, but scenario rules, affordability claims, conversion into records, and acquisition tracking are outside this increment.

## 4. Conceptual data contract

Each record represents one positive amount occurring according to a temporal rule.

| Attribute | Meaning | Rule |
| --- | --- | --- |
| Stable identifier | Identifies the original record across export/reimport | UUID or equivalent, generated by the system |
| Description | Free name such as `Salary A`, `Netflix`, `GPU purchase` | Required |
| Direction | `entrada` (income) or `saida` (expense) in the public pt-BR contract | Required; determines projection sign |
| Per-occurrence amount | Example: `750,00` | Required and positive; stored as integer cents |
| Start | Year/month, with optional day where appropriate | Required; never infer a persisted end date |
| Frequency | Once, daily, weekly, monthly, or yearly | Required; custom intervals may use every N units |
| Occurrence count | Positive integer or unlimited | `1` is one-time; `null` is unlimited |
| Type | Example: Income, Purchase, Subscription, Business | Editable classification only |
| Category | Example: Work, Hardware, Housing | Editable classification only |
| Notes | Optional free context | Never changes projection behavior |
| Technical timestamps | Created/updated | Basic technical audit, never financial dates |

Suggested storage uses `records`, `types`, and `categories` with versioned migrations. Types/categories may be optional; the UI then shows the pt-BR equivalents of “No type” and “No category.” Renaming or deleting a classification must not delete records.

### Canonical example

A GPU purchase paid in ten monthly occurrences of R$750 beginning in January 2027:

```yaml
descricao: Compra de GPU
direcao: saida
valor_por_ocorrencia: 750.00  # UI/file; SQLite stores 75000 cents
inicio: 2027-01
frequencia: mensal
intervalo: 1
quantidade: 10
tipo: Compra
categoria: Hardware
```

Do not store `7500`, a derived `2027-10` end, ten rows, or payment flags. The derived series total is R$7,500.00: R$750.00 from January through October and R$0.00 in November, excluding other expenses.

The same model also represents:

- R$8,000 monthly unlimited salary: income, `800000` cents, monthly, `quantidade=null`.
- R$59.90 monthly unlimited streaming: expense, `5990` cents, monthly, unlimited.
- R$2,000 one-time sale: income, `200000` cents, `quantidade=1`.
- Six R$3,000 contract receipts: income, `300000` cents, monthly, `quantidade=6`.

The UI may optionally accept either per-occurrence value or total plus count. The latter must convert to an exact cent amount before persistence. Do not persist a second total field. If division is not exact in cents, explain the restriction or ask for the per-occurrence value; never hide a remainder.

## 5. Recurrence engine — single calculation source

Conceptual contract:

```text
project(records, period_start, period_end, filters)
  -> virtual occurrences + aggregations
```

- The query interval is finite and calendar-based, never elapsed seconds.
- Calculate only occurrences intersecting the requested interval.
- Income adds `+amount`; expense adds `-amount`.
- Stored amounts are always positive. Direction supplies the sign.
- A record total in a range is `per_occurrence_amount × occurrence_count_in_range`, with the direction sign.
- Projected result equals expected income minus expected expenses.
- A finite series generates only its first N events. Its end is derived, never stored.
- An unlimited series has no absolute total; it only has a total inside a finite query.
- No cron job or worker creates future records. Calculations are pure, testable, and reproducible.

### Civil-calendar rules

- Month: first through last day of the selected month.
- Semester: January–June or July–December of the reference year.
- Year: January–December.
- Navigation moves 1 month, 6 months, or 1 year according to the selected period.
- A start may have `YYYY-MM` precision. Daily and weekly frequencies require an exact day.
- When monthly/yearly records omit the day, never display an invented due date. A daily chart may use a clearly disclosed visual approximation such as the first of the month; it is not a recorded date.
- A 29th/30th/31st anchor moves to the last available day in shorter months without changing its original anchor: Jan 31 → Feb 28/29 → Mar 31.
- Annual Feb 29 recurrence uses Feb 28 in non-leap years and returns to Feb 29 in leap years.
- Use civil-date types without timezone semantics, such as `DateOnly`. Avoid UTC/DST recurrence bugs.

### Chart semantics

The mock's illustrative “projected balance” is larger than its period result. Seryn has no starting bank balance, so production must correct the value semantics while preserving the approved card and composition:

- Plot cumulative expected result within the selected period, starting at zero.
- The final chart value must equal the period result.
- When daily placement is inappropriate because dates omit a day, aggregate by month while preserving the chart block and explain the temporal axis.
- Never use labels promising bank balance, availability, or spendable money.

### Required numeric tests

| Case | Query | Expected result |
| --- | --- | ---: |
| R$750 expense, monthly, 10 occurrences, starts Jan 2027 | January | −R$750 |
| Same record | June | −R$750 |
| Same record | November | R$0 |
| Same record | first semester 2027 | −R$4,500 |
| Same record | second semester 2027 | −R$3,000 |
| Same record | full year 2027 | −R$7,500 |
| R$8,000 unlimited monthly income from January | first semester | +R$48,000 |
| R$2,000 one-time February income | March | R$0 |

Also test counts of 1 and large counts, year boundaries, every-N-month intervals, weekly frequency, day 31, February/leap years, both directions, and empty periods. Monetary arithmetic always uses integer cents; never use floating-point arithmetic.

## 6. Import, export, and backup

### Source of truth

SQLite is the operational source of truth. Import creates/updates original records in SQLite. Export produces a portable snapshot of original records, not expanded virtual occurrences.

### File round-trip

- CSV is UTF-8 with headers. Prefer `;` for pt-BR, allow configuration/autodetection, accept values such as `750,00`, and parse thousands separators correctly.
- Import/export `.xlsx` and legacy `.xls` with appropriate libraries and compatibility tests. Document any legacy limitation honestly.
- Export stable IDs, description, direction, per-occurrence value, start date, frequency, interval, limit, type, category, and notes. Never export a calculated total as the original value.
- Import the same fields with column mapping and preview before commit. Partially validated data must never compromise the database.
- Known IDs allow confirmed upsert; new IDs create records. Missing IDs require explicit possible-duplicate handling. Reimporting an intact export must not silently duplicate records.
- Preview classifications that do not exist and allow controlled creation.
- CSV must open safely in Brazilian Excel and neutralize formula injection in text fields.
- Reject invalid amounts, zero/negative counts, unknown frequencies, and incompatible dates with intelligible row errors. Summarize accepted/rejected rows.
- Limit upload size/volume and validate all input.

Illustrative CSV:

```csv
id;descricao;direcao;valor;inicio;frequencia;intervalo;quantidade;tipo;categoria
;Compra de GPU;saida;750,00;2027-01;mensal;1;10;Compra;Hardware
;Salário A;entrada;8000,00;2027-01;mensal;1;;Renda;Trabalho
```

Empty `quantidade` means unlimited; `quantidade=1` means one occurrence. Missing IDs are generated. Internal column names may differ only if public round-trip remains equivalent and documented.

Expanded projection export, if added in v0.1, is a separate lower-priority operation named clearly as a period projection. It must not be mistaken for reimportable original records.

### Database recovery and migrations

- Store the database at `/data/seryn.sqlite` and backups under `/data/backups/`.
- Enable foreign keys, use versioned EF Core migrations, and prefer WAL.
- Use SQLite backup APIs, `VACUUM INTO`, or an equivalent consistent method that accounts for WAL. Never trust a plain copy of an open database file.
- Restore through an explicit replacement flow with a warning and file validation.
- Container recreation/update must never delete data.
- Document backup, export, and recovery for Unraid.

## 7. Approved v2 design and experience

`docs/design/prototypes/seryn_mock_v2.html` is the primary visual reference, supported by the light/dark desktop/mobile screenshots. It is illustrative source material, not production code. Its figures and period multiplication are fictitious and must never become business logic.

### Desktop structure to preserve

1. Left sidebar: Overview, Records, Categories, Types, Import/Export, Settings (localized in pt-BR in product UI).
2. Overview header, Month/Semester/Year selector, and date navigation.
3. Three cards: Income, Expenses, Result.
4. Central period-projection panel with chart and legend.
5. Category panel with donut and details.
6. Projected-records panel with occurrences in the interval.
7. Light/dark theme control.

Technical alignment and semantic wording corrections are allowed. Do not replace this composition with a generic dashboard.

### Mobile/PWA composition

- Design iPhone widths first; do not shrink desktop cards side by side.
- Compact Seryn header and theme control.
- Overview title, readable date selection, and period control.
- Prominent result first, then income and expenses as two smaller cards or stacked at narrow widths.
- Short initial record list (for example, three occurrences) with a “view all” action.
- Categories available through navigation; the overview need not show the donut.
- Fixed bottom navigation: Overview, Records, Categories, Settings. Types and Import/Export remain explicitly reachable through Settings or another clear route.
- Respect `safe-area-inset-bottom`, keep content above the bar, use readable text, and target roughly 44×44 CSS-pixel touch areas.

### Tokens and identity

Visual tone: refined, light, modern, clear, and restrained, with rounded corners and subtle translucency. Contrast takes priority over glass effects. Each theme has deliberate tokens:

| Token | Light | Dark |
| --- | --- | --- |
| Background | `#F2F4F5` | `#111719` |
| Surface | translucent white | translucent `#202A2C` |
| Text | `#1B292D` | `#F1F6F5` |
| Secondary text | `#5B6B70` | `#BAC9C7` |
| Accent | `#117E78` | `#75D1C5` |
| Income | `#087E57` | `#80DEB6` |
| Expense | `#BB3D58` | `#FF94A7` |

Use CSS variables and reusable components. Never communicate direction through color alone; pair it with labels/signs/icons. Meet WCAG AA for text, provide visible keyboard focus, honor reduced-motion preferences, remember explicit theme choice, and otherwise follow the system theme. Avoid heavy decorative animation.

### UI behavior

- Empty state says that the period has no records and never simulates totals.
- Filters and period changes update cards, chart, categories, and records from the same filtered data.
- Format BRL using pt-BR, while storing cents as integers.
- “Projected records” means virtual expected occurrences, not paid/due items.
- Every visible navigation item/button works. Secondary pages may begin simple if mandatory flows remain usable.

## 8. Approved architecture (ADR-001)

The fixed decision is React + TypeScript in the browser and C# on ASP.NET Core 10 LTS in a modular monolith. SQLite uses EF Core 10. Node is build-only. Do not use Node/Fastify/Express/NestJS, Laravel, or Spring Boot as the backend.

Do not create microservices, queues, Redis, remote databases, multiple servers, or mandatory third-party dependencies. Do not adopt Clean Architecture, DDD, CQRS, or MediatR by default; organize only the modules the application needs.

| Layer | Technology | Constraint |
| --- | --- | --- |
| Frontend | React + TypeScript + Vite | v2 UI and PWA |
| UI | Tailwind CSS + shadcn/ui patterns | Custom tokens, accessibility, fidelity |
| Backend | C# + ASP.NET Core 10 LTS | Minimal APIs or controllers |
| HTTP | Kestrel | Only production server/process |
| API | Versioned REST JSON under `/api/v1` | Stable contracts and generated OpenAPI |
| Validation | Typed, centralized backend validation | Shared by every input origin |
| Persistence | SQLite + EF Core SQLite 10 | Versioned migrations |
| Recurrence | Pure C# module | No HTTP, React, or EF dependency |
| Money | `long` integer cents | Never float/double |
| Financial dates | Civil dates and explicit calendar | `DateOnly` where a day exists |
| CSV | CsvHelper or established equivalent | Culture and formula safety |
| `.xlsx` | ClosedXML or equivalent | Tested read/write |
| `.xls` | NPOI/HSSF or equivalent | Do not claim support without tests |
| Distribution | Multi-stage Docker | One image, process, container, `/data` volume |

Native AOT and chiseled images are not required. Use official, patched .NET 10 SDK/ASP.NET images. Produce linux/amd64, and arm64 when builds/tests permit.

Production topology:

```text
Desktop / iPhone PWA / browser
               │ same-origin HTTPS
               ▼
┌────────────────────────────────┐
│ One Seryn container            │
│ ASP.NET Core 10 + Kestrel      │
│  ├─ compiled React in wwwroot  │
│  ├─ REST API /api/v1           │
│  ├─ validation/services        │
│  ├─ pure recurrence engine     │
│  └─ EF Core + SQLite           │
└───────────────┬────────────────┘
                ▼
        /data persistent volume
```

The Docker build compiles React in a Node stage, builds/publishes .NET in an SDK stage, and copies only published output into the ASP.NET runtime. No Node, Nginx, PHP, or worker remains. API and frontend share origin/port. SPA fallback must never hide API 404s.

Suggested repository structure keeps one server project and one test project, with logical Domain, Projections, Persistence, ImportExport, and Api modules. Keep projections independent of EF and HTTP without turning modules into deployment services.

### Shared application flow

UI, file import, and future integrations must all use the same application service and validation. Import converts rows into the same internal command and writes transactionally; it never bypasses rules with direct database writes. Future Aira/external adapters and API credentials remain outside v0.1.

Reference REST surface:

```text
GET/POST                 /api/v1/records
GET/PATCH/DELETE         /api/v1/records/{id}
GET/POST                 /api/v1/types
PATCH/DELETE             /api/v1/types/{id}
GET/POST                 /api/v1/categories
PATCH/DELETE             /api/v1/categories/{id}
GET                      /api/v1/projections?period=month|semester|year&reference=YYYY-MM
GET                      /api/v1/analyses?period=month|semester|year&reference=YYYY-MM&direction=all|income|expense&primary=category|payment|type&secondary=none|category|payment|type
GET/POST                 /api/v1/objectives
GET/PATCH/DELETE         /api/v1/objectives/{id}
POST                     /api/v1/import/preview
POST                     /api/v1/import/commit
GET                      /api/v1/export/records?format=csv|xlsx|xls
POST/GET                 /api/v1/backups
POST                     /api/v1/backups/restore
```

Names may vary modestly, but `/api/v1`, semantics, authentication, validation, DTO isolation, safe Problem Details, and OpenAPI are required. React calls authenticated HTTP and never queries SQLite.

### SQLite and operations

- Keep database/backups under `/data` and stream exports to the browser when practical.
- Apply migrations; never use `EnsureCreated` in production with migrations.
- Enable foreign keys, consider WAL/busy timeout, and use transactions for confirmed imports.
- Back up consistently and validate restore. Consider pre-migration backup where necessary.
- Run unprivileged with writable `/data`. Health checks expose no private data.
- Log useful technical events without indiscriminately logging descriptions or values.
- Do not persistently cache private API responses in the frontend.

### Security and authentication

- One local user with a strong password hash and secure session. No financial page or endpoint is anonymous.
- Initial provisioning must prevent remote takeover. Local/protected setup or a temporary provisioning secret is acceptable.
- Cookies are HttpOnly, Secure under HTTPS, and appropriately SameSite. Protect cookie-authenticated mutations from CSRF, and address XSS, brute force, path traversal, and upload limits.
- Support LAN/Tailscale and trusted HTTPS proxies without requiring a specific provider. Trust forwarded headers only from configured proxies.
- Authentication exists before real financial data and CRUD are exposed.
- PWA caching is limited to safe static assets. v0.1 has no offline writes or synchronization.

### Operational documentation

Document Compose build/run, internal port 8080, `/data`, health checks, upgrades, logs, backup/restore, and HTTPS proxying. Never commit secrets or a real SQLite database. Provide `.env.example` without credentials. Container replacement must preserve migrations, frontend, and data.

## 9. Implementation phases

### Phase 1 — Foundation, basic security, and theme

- Compiling React/Vite and ASP.NET Core 10 projects; multi-stage Docker/Compose; `/data`; SQLite and EF migrations.
- One .NET server serves compiled frontend, `/api/v1`, and health endpoint.
- Secure local single-user provisioning and authentication before financial data.
- Approved v2 desktop/mobile shell, navigation, and light/dark tokens.
- PWA manifest and accessible mobile navigation, without private persistent caching.
- Empty state in normal mode; no fictional figures.

Acceptance: `docker compose up --build` starts successfully; SQLite survives restart; UI follows v2; theme works; reserved financial endpoints are protected. No Aira integration is required.

### Phase 2 — Data and recurrence

- Record/type/category CRUD.
- Record validation, integer cents, and civil dates.
- Pure recurrence engine with tests and projection API.
- Persisted-data dashboard with working filters and periods.

Acceptance: all Section 5 numeric examples pass and chart end equals income minus expenses.

### Phase 3 — Portability and robustness

- CSV/XLSX/XLS import/export, preview, row errors, duplicates, and round-trip.
- Backup/restore and volume documentation.
- Authentication/session/upload security tests.
- Integration, mobile UX, Unraid, and upgrade documentation.

Acceptance: export → clear test database → import reproduces records/projections; container replacement preserves data; anonymous access remains impossible.

### Current extension — analyses

- Add the responsive Análises destination to desktop and mobile navigation.
- Query finite projected occurrences by period and direction.
- Segment by category, payment method, or type, with an optional second-dimension crossing.
- Drill each segment down to its original records without creating analysis rows in SQLite.

Acceptance: analysis totals equal projections for the same period and direction; percentages use the documented volume basis; occurrence and original-record counts remain distinct; finite recurrence progress reports projected installments without implying payment; category, payment, and type crossings are derived from persisted records.

### Current extension — objectives

- Add authenticated objective CRUD with versioned SQLite persistence.
- Add responsive list, search, interest filter, alternative sorting, creation, editing, and deletion flows.
- Keep objectives isolated from financial projections and analyses.

Acceptance: required and optional fields follow the approved contract; interest remains a classification rather than a workflow; priority accepts only `-3` through `+3`; inclusion date survives edits and restarts; objective mutations do not change any financial result.

### Phase 4 — v0.1 polish

- Validate Safari/iOS rendering and PWA installation.
- Review keyboard, focus, contrast, touch, scrolling, and safe areas.
- Review wording to avoid paid/pending semantics.
- Confirm every mock control is implemented or has a clear destination.

## 10. Final acceptance checklist

- Salary and subscription use one record model.
- A ten-month R$750 purchase stores one row, not ten.
- R$7,500 total is calculated, never persisted as truth.
- Unlimited recurrence never expands outside a finite query.
- Result equals expected income minus expected expenses under filters.
- Chart assumes no prior balance and ends at the period result.
- Semesters are Jan–Jun/Jul–Dec; series do not leak before start or after finite end.
- No payment status exists in database, API, or UI.
- Editable classifications do not alter calculations or destroy records.
- CSV/Excel round-trip preserves unit value, recurrence, and classifications.
- Reimport never silently duplicates; row errors are explicit.
- Restored SQLite reproduces data.
- One Docker container/process serves React and `/api/v1`, with SQLite under `/data`.
- Final image contains no Node runtime or auxiliary server; EF Core 10 migrations are versioned.
- OpenAPI documents `/api/v1`; UI and import share validation/application services.
- Local authentication protects data before first use and cannot be remotely claimed.
- Desktop/mobile match approved v2 composition in both themes.
- Production contains no hardcoded financial data.
- PWA installs and works on common iPhone/Safari widths.
- README covers Unraid, backup, import/export status, and product limits.

## 11. Fixed decisions and future work

Minor component choices, module folders, forms, and spreadsheet libraries are engineering details so long as they meet this contract. Do not change without discussion: React + ASP.NET Core 10 + EF Core 10 + SQLite; one modular monolith/container; per-occurrence integer value; no payment state; month/semester/year periods; one record model; exportable originals; v2 desktop/mobile composition; independent identity; all values forecast-only.

Future possibilities outside v0.1 include advanced grouping, alternative scenarios, period comparisons, offline synchronization, multiple currencies/users, external importers, and ecosystem integrations. Do not anticipate them artificially.

Items to validate later without blocking foundation work: real legacy `.xls` fidelity, physical Safari/iOS PWA/safe-area behavior, access recovery strategy, and WAL-aware backup integrity. Never mark an untested capability complete.

## 12. Contributor handoff

Read this entire document and inspect the v2 prototype/screenshots before coding. Work in small executable increments. Never invent data, screens, or financial rules. Record safe technical decisions; discuss product-rule changes. Run lint, builds, and relevant tests before declaring completion. Every delivery reports what works, how to test it, and what remains.

Guiding criterion: Seryn lets a person record or import an expected income/expense once and obtain reliable period projections without the complexity or semantics of banking software.
