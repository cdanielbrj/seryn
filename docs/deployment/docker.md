# Docker operations

## Start

```sh
cp .env.example .env
# Set a unique SERYN_ADMIN_PASSWORD before the first start.
docker compose up --build -d --wait
```

Open `http://localhost:8080`. The default publishes only to host loopback.

## Shared development instance

The repository workspace keeps the Compose project `seryn` available at `http://localhost:8080`:

```sh
docker compose -p seryn up --build -d --wait
```

Container recreation is allowed. Preserve the `seryn_seryn-data` volume, credentials, and existing data. Keep this instance running after development tasks. Destructive and end-to-end tests must use a separate project and disposable volume.

## Operations

```sh
docker compose ps
docker compose logs --tail=100 seryn
curl --fail http://localhost:8080/health
docker compose restart seryn
docker compose up -d --force-recreate --wait
docker compose down
```

`down` preserves the named volume. Never use `down -v` for an installation whose data must survive.

Update a local build with:

```sh
docker compose build --pull
docker compose up -d --wait
```

## Persistent data and backup

The database, administrator, sessions, and Data Protection keys live under `/data`. Rebuilding the image does not remove them.

Before upgrades that include migrations, keep a consistent copy of the volume. Until the application provides a validated online backup flow, stop the container and copy the complete volume with the host backup tool. Include SQLite auxiliary files and keys; never copy only `seryn.sqlite` while WAL mode is active. Restore only while the application is stopped and preserve ownership and permissions.

## Image architecture

Build an amd64 image explicitly on an arm64 host with:

```sh
docker buildx build --platform linux/amd64 --load -t seryn:amd64 .
```
