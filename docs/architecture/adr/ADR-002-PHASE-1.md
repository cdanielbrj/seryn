# ADR-002 — Phase 1 foundation

Status: implemented. Scope: Phase 1 only.

## Structure

One ASP.NET Core 10 project contains the internal `Auth`, `Api`, and `Persistence` modules. The React/TypeScript/Vite frontend uses Tailwind CSS 4, a shadcn/ui-style Button built on Radix Slot, and tokens taken from the approved v2 mock. Files under `docs/design/prototypes/` remain reference artifacts.

Minimal APIs expose DTOs under `/api/v1`. The server generates OpenAPI at `/api/v1/openapi/v1.json`, available only to an authenticated session. The SPA fallback handles React routes, while unknown API routes return Problem Details. Phase 1 has no CRUD or projection endpoints. The authenticated `/api/v1/foundation` endpoint explicitly reports that financial data is unavailable.

npm dependencies are locked. A development-only override requires `minimatch >=11.0.1` to resolve GHSA-g3hv-75wg-m249 in lint tooling; lint, tests, and build verified compatibility. Development tooling is absent from the final image.

## Initial administrator and sessions

- Startup provisions the administrator from `SERYN_ADMIN_USER` (default `admin`) and `SERYN_ADMIN_PASSWORD`, or from a file named by `SERYN_ADMIN_PASSWORD_FILE`. There is no default password, setup code, or public setup endpoint.
- If SQLite already contains a user, provisioning returns before reading or validating the variables or file. Stored credentials, sessions, and lockouts always win, including after restore or an upgrade from the previous setup-code flow. Environment variables are not a password-reset mechanism.
- With no initial password, the server starts with access locked, a clear UI state, and a safe operational warning. Explicit invalid credentials or conflicting password sources fail startup. The user hash is committed before HTTP requests are accepted, and credentials are never logged.
- `local_user` constrains its primary key to `1`, preventing a second account. A process-wide semaphore serializes login counter updates. The supported topology is one process per volume. An obsolete `bootstrap.token` is removed during upgrade.
- Passwords contain 16–128 characters and use ASP.NET Core Identity's V3 password-hash format with PBKDF2 and 210,000 iterations. No external identity provider or recovery service is required.
- The HttpOnly, SameSite=Strict session cookie lasts up to eight hours without sliding renewal. A revocable SQLite session is checked on every authenticated request. Logout deletes the server-side session before removing the cookie.
- Data Protection keys persist in `/data/keys`; `/data` and the keys directory use Unix mode `0700`. Keys are not additionally encrypted at rest, so host and volume security remain operational requirements.
- Every mutating `/api` request, including login, requires antiforgery validation. The frontend obtains a token for the current identity before each POST. No permissive CORS policy exists.
- Five failed logins lock the account for five minutes, and counters survive restart. Login also uses a global limit of ten attempts per minute, appropriate for the single-user service and independent of spoofable client IP headers.
- The server requires HTTPS by default. Compose explicitly selects HTTP while binding to loopback for local development. Cookies are always Secure in HTTPS mode. Forwarded headers are accepted only from loopback and exact configured proxy addresses.
- API and session responses use `Cache-Control: no-store`. The first trusted proxy must overwrite client-supplied forwarding headers.

## Persistence and distribution

The `Foundation` EF migration creates only the single user and sessions, plus EF migration metadata. No financial table is introduced before Phase 2. SQLite enables foreign keys, WAL, and a ten-second busy timeout. Startup applies versioned migrations; production never uses `EnsureCreated`.

The final image contains ASP.NET Core and compiled web assets, with no Node, Nginx, or auxiliary server. The healthcheck starts a short .NET command that calls `/health`; it is not a resident service. Kestrel runs as the unprivileged `app` user. Compose limits writable storage to `/data` and `/tmp`, drops Linux capabilities, and disallows privilege escalation.

## UI and PWA

Desktop preserves the v2 sidebar, three summary cards, projection panel and legend, category distribution, and expected-records panel. Mobile puts the result first, uses two smaller income/expense cards, shows a short records list, and provides a safe-area-aware fixed bottom navigation. Secondary routes have explicit destinations without implementing later-phase forms.

Amounts appear as an em dash and panels use empty states because financial queries do not exist yet. The implementation contains no demo amounts, starting balance, or calculations copied from the mock. Month, calendar-semester, and year controls navigate civil periods without projecting data.

Theme selection follows the OS until explicitly chosen and stores only that preference in localStorage. The app includes a manifest, 192/512 PNG icons, a maskable icon, and an Apple 180 icon. It deliberately has no service worker, Cache Storage, offline writes, or synchronization. Physical Safari/iPhone installation remains a Phase 4 validation item.

## Release automation

`Directory.Build.props` is the single release-version source. Pull requests and pushes to `main` validate backend tests, frontend lint/tests/build, and the final image. Successful `main` pushes publish `latest` and commit-SHA multi-platform images to GHCR. When `<Version>` changes, the workflow creates an immutable annotated `vX.Y.Z` tag after validation and publication. It does not create a GitHub Release.

## Deliberate limits

Record CRUD, the pure recurrence engine, and canonical numeric cases belong to Phase 2. CSV/Excel portability, application-driven backup/restore, and access recovery belong to later work. Phase 1 tests only behavior that is actually implemented.
