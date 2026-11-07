# Seryn

Seryn is a self-hosted planner for expected income and expenses. Phase 1 is complete and Phase 2 is in progress, following the [product specification](docs/SERYN_SPEC.md).

Available today: secure administrator provisioning, login/logout, persistent local sessions, EF Core migrations on SQLite, editable types/categories/payment methods, financial-record CRUD, deterministic projections, the data-backed dashboard, period navigation, light/dark themes, and an installable web-app manifest. Import/export and in-app backup remain reserved for Phase 3.

Current version: **0.1.0**.

## Run with Docker

Requirements: Docker with Compose v2. From the repository root:

```sh
cp .env.example .env
# Edit .env and set SERYN_ADMIN_USER and a unique 16–128 character SERYN_ADMIN_PASSWORD.
docker compose up --build -d --wait
```

Open **http://localhost:8080**. The default binds only to host loopback. One ASP.NET Core/Kestrel process serves the compiled React application and `/api/v1`; Node exists only in the build stage. The unprivileged runtime container stores SQLite and Data Protection keys in the `seryn-data` volume mounted at `/data`.

### Shared development instance

This workspace keeps the Compose project `seryn` running at **http://localhost:8080** for ongoing validation. Its container is `seryn-seryn-1` and its persistent volume is `seryn_seryn-data`. Update it with:

```sh
docker compose -p seryn up --build -d --wait
```

Compose may briefly replace the container, while preserving credentials and data in the volume. `restart: unless-stopped` brings the application back when Docker restarts unless it was stopped manually. Keep this instance running after development tasks. Use a separate Compose project and disposable volume for destructive or end-to-end tests; never run `down -v` against the shared instance.

### Initial administrator

The server provisions the single administrator during startup:

| Variable | Purpose |
| --- | --- |
| `SERYN_ADMIN_USER` | Initial administrator name; defaults to `admin`. Must contain 1–64 non-control characters. |
| `SERYN_ADMIN_PASSWORD` | Unique initial password with 16–128 characters. There is no default password. |
| `SERYN_ADMIN_PASSWORD_FILE` | Alternative to the direct password: a secret-file path **inside the container**. |

Fill `.env`, then run `docker compose -p seryn up --build -d --wait`. Single-quote `.env` values containing `$`, `#`, or spaces to prevent Compose interpolation. `.env` remains local, is ignored by Git, and is excluded from the Docker build context.

Open Seryn and sign in directly. There is no setup code and no account-creation endpoint. When no password is configured, the UI remains locked and explains that server configuration is required. Invalid explicit credentials fail startup without printing the supplied values.

An administrator already stored in SQLite always wins, including after a database restore. Changing or removing environment variables does not alter the existing name, password hash, sessions, or lockout counters. This also preserves accounts created by the earlier setup-code flow; obsolete `bootstrap.token` files are removed on upgrade.

After initial provisioning, you may remove the password variable and recreate the container. The password hash remains in SQLite. The variables provision a new database; they are not a password-reset mechanism. Keep the password in a password manager, because Phase 1 has no recovery workflow.

#### Secret file

Use exactly one password source. For a Compose secret, leave `SERYN_ADMIN_PASSWORD` empty and set `SERYN_ADMIN_PASSWORD_FILE=/run/secrets/seryn_admin_password`:

```yaml
services:
  seryn:
    secrets:
      - seryn_admin_password
secrets:
  seryn_admin_password:
    file: /local/path/admin-password.txt
```

The file must be readable by container UID 1654, contain at most 1024 bytes, and contain only the password. The reader removes trailing LF/CRLF characters only; spaces remain part of the password. Never commit this file. It is no longer required after provisioning succeeds.

### Operations

```sh
docker compose ps
docker compose logs --tail=100 seryn
curl --fail http://localhost:8080/health
docker compose restart seryn
docker compose up -d --force-recreate --wait
docker compose down
```

`down` preserves the volume. Do not use `down -v` for an installation you want to keep. The database, user, sessions, and keys live under `/data`; rebuilding the image does not remove them. `/health` exposes only a public technical status.

Update from local source with:

```sh
docker compose build --pull
docker compose up -d --wait
```

Before upgrading across migrations, keep a consistent copy of the volume. Phase 1 does not have an online backup API. For an administrative backup, stop the container and copy the complete volume—including keys and SQLite auxiliary files—with the host's backup tool. Restore it only while the application is stopped and preserve ownership/mode. Never copy only `seryn.sqlite` while the application is using WAL. A validated online backup/restore flow belongs to Phase 3.

## Unraid and bind mounts

An Unraid template is available at [`unraid/seryn.xml`](unraid/seryn.xml). After it is pushed to `main`, in Unraid open **Docker → Add Container → Template repositories** and add `https://raw.githubusercontent.com/cdanielbrj/seryn/main/unraid/seryn.xml`; then choose **Add Container** and select Seryn. The template uses the published multi-architecture image `ghcr.io/cdanielbrj/seryn:latest`.

Create the appdata directory and assign it to UID/GID `1654`, used by the official ASP.NET runtime image:

```sh
mkdir -p /mnt/user/appdata/seryn
chown 1654:1654 /mnt/user/appdata/seryn
chmod 700 /mnt/user/appdata/seryn
```

Replace only the service volume:

```yaml
volumes:
  - /mnt/user/appdata/seryn:/data
```

Do not use privileged mode or `777` permissions. Configure the port, hostname, and HTTPS topology as described below. Import/export and UI-driven restore are not available yet.

### LAN and HTTPS proxy

Remote access and iPhone PWA installation should use an HTTPS proxy you administer. Seryn does not require a particular provider. Replace these examples with the real hostname and proxy IP:

```dotenv
SERYN_BIND_ADDRESS=0.0.0.0
SERYN_PORT=8080
SERYN_ALLOWED_HOSTS=seryn.example.local
SERYN_REQUIRE_HTTPS=true
SERYN_TRUSTED_PROXY=192.168.1.10
```

Use a firewall to restrict port 8080 to the proxy. The proxy must preserve `Host`, overwrite `X-Forwarded-Proto` with `https`, and manage `X-Forwarded-For`. Configure the IP that the container actually sees as the proxy source; never trust an arbitrary network. Add `Seryn__TrustedProxies__1`, and so on, for additional exact proxy addresses. HTTP API requests are rejected when HTTPS mode is enabled.

On a trusted LAN or Tailscale network, you may explicitly select `SERYN_REQUIRE_HTTPS=false` and bind to the appropriate local address. This sends credentials without transport encryption. The default local configuration exposes no external port.

On iPhone Safari, open the HTTPS URL, choose Share, then Add to Home Screen. The manifest uses standalone mode, includes dedicated icons, and the bottom navigation accounts for safe areas. There is no offline mode or persistent cache of private responses. Physical iPhone validation remains part of Phase 4.

## Local development

Requirements: **.NET 10 SDK**, **Node 24 LTS**, and npm. SQLite does not need a separate installation.

Backend terminal:

```sh
dotnet tool restore
dotnet restore Seryn.slnx
# Export SERYN_ADMIN_USER and SERYN_ADMIN_PASSWORD (or SERYN_ADMIN_PASSWORD_FILE) first.
# dotnet run does not load the Compose .env file.
dotnet run --project src/server
```

The development profile uses `http://127.0.0.1:8080`, explicit local HTTP, and `.data/` in the repository root. Migrations run at startup. When using the shared Compose instance, it can provide the API to Vite without starting another backend on the same port.

Frontend terminal:

```sh
cd src/web
npm ci
npm run dev
```

Open `http://127.0.0.1:5173`. Vite proxies `/api` and `/health` to the backend so the browser still uses one origin.

To serve the compiled frontend directly from local Kestrel:

```sh
npm --prefix src/web run build
mkdir -p src/server/wwwroot
cp -R src/web/dist/. src/server/wwwroot/
dotnet run --project src/server
```

`wwwroot` is a generated, ignored artifact. Rebuild and copy it after UI changes.

## Validation

```sh
dotnet build Seryn.slnx -c Release
dotnet test Seryn.slnx -c Release
dotnet format Seryn.slnx --verify-no-changes
dotnet ef migrations has-pending-model-changes --project src/server
npm --prefix src/web ci
npm --prefix src/web run lint
npm --prefix src/web test
npm --prefix src/web run build
```

Without local SDKs, use the Docker test stages:

```sh
docker build --target server-test -t seryn-server-test .
docker build --target web-test -t seryn-web-test .
docker compose up --build -d --wait
```

The final Docker build runs frontend lint, unit tests, and production build. The `server-test` stage runs the backend integration suite against temporary real SQLite databases with migrations. See the [Phase 1 validation record](docs/PHASE_1_VALIDATION.md) for executed checks.

Generate an amd64 image explicitly on an arm64 host with:

```sh
docker buildx build --platform linux/amd64 --load -t seryn:amd64 .
```

### Browser tests

Use a disposable installation only. Provision its container with `SERYN_ADMIN_USER=e2e` and `SERYN_ADMIN_PASSWORD=Seryn teste descartavel 123!` (test-only credential). The E2E setup verifies that the administrator exists; it never creates an account through the API.

```sh
cd src/web
npx playwright install --with-deps chromium webkit
SERYN_E2E=1 \
SERYN_E2E_URL=http://127.0.0.1:18080 \
npm run test:e2e
```

Chromium desktop and WebKit with an iPhone viewport exercise login, themes, navigation, deep links, periods, and logout. Screenshots and reports are written under the ignored `tests/web/artifacts/` directory. Automated WebKit does not replace physical Safari/iPhone validation.

## API

- `GET /health`: public technical health.
- `GET /api/v1/auth/status` and `GET /api/v1/auth/csrf`: public and contain no personal data.
- `POST /api/v1/auth/login`: CSRF-protected and rate-limited. No initial account-creation endpoint exists.
- `GET /api/v1/auth/session`, `POST /api/v1/auth/logout`, and `GET /api/v1/foundation`: authenticated.
- `GET/POST /api/v1/records` and `GET/PATCH/DELETE /api/v1/records/{id}`: authenticated record CRUD.
- `/api/v1/types`, `/api/v1/categories`, and `/api/v1/payment-methods`: authenticated classification CRUD.
- `GET /api/v1/projections?period=month|semester|year&reference=YYYY-MM`: authenticated derived dashboard projection.
- `GET /api/v1/openapi/v1.json`: server-generated OpenAPI, authenticated.

Mutating calls send the session cookie and the `X-CSRF-TOKEN` value obtained from `/auth/csrf`. Errors use Problem Details. All financial paths return 401 without a session. Import/export and backup handlers remain unavailable until their corresponding increments.

## Releases

The version lives in `Directory.Build.props`. Pull requests and pushes to `main` build both test stages and the production image. A successful push to `main` publishes multi-architecture images to:

```text
ghcr.io/<owner>/<repository>:latest
ghcr.io/<owner>/<repository>:<commit-sha>
```

When a commit changes `<Version>`, the successful workflow creates one annotated `vX.Y.Z` Git tag. Existing tags are never moved. The workflow does not create a GitHub Release; that remains a separate explicit action. The initial successful `main` build creates `v0.1.0` because the previous revision has no version file.

Implementation decisions are recorded in [ADR-002](docs/ADR-002-PHASE-1.md), [ADR-003](docs/ADR-003-PHASE-2-RECORDS.md), and [ADR-004](docs/ADR-004-DASHBOARD-PROJECTIONS.md). No later phase is declared complete.
