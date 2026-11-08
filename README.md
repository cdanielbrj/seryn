# Seryn

Seryn is a self-hosted planner for expected income and expenses. It stores original planned records and derives recurrences, totals, and projections without persisting virtual future installments.

Current version: **0.2.0**.

Available today: secure single-administrator provisioning, local sessions, editable classifications, financial-record CRUD, deterministic projections, segmented analyses, independent objectives, a data-backed responsive dashboard, and light/dark presentation. Import/export and application-managed backup remain future increments.

## Quick start

Requirements: Docker with Compose v2.

```sh
cp .env.example .env
# Set a unique 16–128 character SERYN_ADMIN_PASSWORD.
docker compose up --build -d --wait
```

Open [http://localhost:8080](http://localhost:8080).

One unprivileged ASP.NET Core process serves the compiled React application and `/api/v1`. SQLite, sessions, and Data Protection keys persist in the `seryn-data` volume mounted at `/data`. Node is present only during the image build.

The provisioning variables initialize a new database; they never reset an administrator already stored in SQLite. Keep the initial password in a password manager.

## Documentation

- [Product specification](docs/product/SERYN_SPEC.md)
- [Documentation index](docs/README.md)
- [Architecture and ADRs](docs/architecture/README.md)
- [Design references](docs/design/README.md)
- [Deployment](docs/deployment/README.md)
- [Local development](docs/development/README.md)
- [Validation records](docs/validation/README.md)

Source-specific contributor guidance:

- [Backend architecture](src/server/README.md)
- [Frontend application architecture](src/web/app/README.md)
- [Theme architecture](src/web/themes/README.md)

## Development validation

The repository provides independent Docker test stages:

```sh
docker build --target server-test -t seryn-server-test .
docker build --target web-test -t seryn-web-test .
```

See [testing](docs/development/testing.md) for host commands, browser tests, and validation boundaries.

## Releases

The version lives in `Directory.Build.props`. Successful pushes to `main` build both test stages and publish multi-architecture images as `latest` and the commit SHA. A version change creates one immutable annotated `vX.Y.Z` tag after the workflow succeeds.

## License

No license has been declared yet. All rights remain with the repository owner until a license is added.
