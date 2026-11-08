# Local environment

Local host development requires .NET 10 SDK, Node 24 LTS, and npm. SQLite requires no separate installation.

## Backend

```sh
dotnet tool restore
dotnet restore Seryn.slnx
# Export administrator provisioning variables before the first run.
dotnet run --project src/server
```

The development profile uses `http://127.0.0.1:8080` and stores data under the ignored repository `.data/` directory. `dotnet run` does not load the Compose `.env` file.

## Frontend

```sh
cd src/web
npm ci
npm run dev
```

Open `http://127.0.0.1:5173`. Vite proxies `/api` and `/health` to the backend. The shared Compose instance may provide that backend when another local server is not running on port 8080.

To serve a freshly compiled frontend from local Kestrel:

```sh
npm --prefix src/web run build
mkdir -p src/server/wwwroot
cp -R src/web/dist/. src/server/wwwroot/
dotnet run --project src/server
```

`wwwroot` is generated and ignored. Rebuild and copy it after frontend changes.
