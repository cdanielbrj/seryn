# Phase 1 validation record

Validation ran on November 3, 2026, on Docker Desktop for macOS arm64. Build and test runtimes ran in containers; the host did not require Node or .NET installations.

## Administrator-provisioning revision

The initial setup-code flow was replaced by startup provisioning. The revision passed:

- .NET Release build, React/Vite production build, ESLint with zero warnings, and C# formatting verification.
- 18 backend integration tests covering provisioning, hashing, locked behavior without a password, later provisioning against the same database, preservation when variables change or disappear, file secrets, rejection of conflicts/invalid values, absence of a public setup endpoint, CSRF, lockout, sessions, and persistence.
- 5 frontend unit tests, including CSRF login and a non-configured installation without a remote account-creation form.
- 2 end-to-end flows in desktop Chromium and mobile WebKit against a disposable Compose installation provisioned through environment variables.
- Final arm64 image build and healthy startup through Compose. This behavior change required no schema migration.

## Baseline checks

| Check | Result |
| --- | --- |
| React/TypeScript/Vite production build | Passed |
| ESLint with zero warnings | Passed |
| Vitest | Passed |
| .NET 10 Release build | Passed |
| xUnit against real temporary SQLite databases and migrations | Passed |
| `dotnet format --verify-no-changes` | Passed |
| EF pending-model check | No unversioned model changes |
| Final Linux arm64 image build | Passed |
| Final Linux amd64 image build | Passed |
| amd64 startup and healthcheck on arm64 emulation | Passed |
| `docker compose up --build -d --wait` | Healthy |
| Container replacement over the same volume | Credentials preserved; login passed |
| Final runtime process | Only `dotnet Seryn.Server.dll`, UID/GID 1654; Node absent |
| Manifest served by Kestrel | HTTP 200, `application/manifest+json` |
| Desktop Chromium E2E | Passed |
| Mobile WebKit/iPhone viewport E2E | Passed |
| `npm audit` | Zero reported vulnerabilities |
| NuGet audit, including transitive packages | No vulnerable package reported by configured sources |

Browser coverage includes login, desktop/mobile composition, absence of fictional amounts, period navigation, theme persistence, secondary routes, deep-link reload, logout, browser-history behavior after logout, and absence of Cache Storage. Both theme screenshots are generated under the ignored `src/web/test-results/` path.

## Validation limits

- WebKit with an iPhone viewport is not a physical Safari/iPhone. Installation, Safari chrome behavior, and real safe areas remain Phase 4 work.
- The application has not yet been deployed to real Unraid hardware or an external HTTPS proxy. The README documents the topology and backend tests cover HTTPS enforcement.
- Phase 1 has no record model or financial engine. Canonical numeric projection examples belong to Phase 2 and were not replaced with artificial fixed-value assertions.
- Import/export and application-driven backup/restore are not implemented or claimed as tested.

The shared development instance remains running after validation. Destructive browser suites use a separate Compose project and disposable volume.
