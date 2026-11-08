# Testing

Run checks proportionally to the change, and run the complete affected stack before declaring a delivery complete.

## Host commands

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

## Docker test stages

```sh
docker build --target server-test -t seryn-server-test .
docker build --target web-test -t seryn-web-test .
```

The frontend stage runs lint, unit tests, and the production build. The server stage runs backend integration tests against temporary SQLite databases with migrations.

## Browser tests

Use a disposable installation, never the shared development volume:

```sh
cd src/web
npx playwright install --with-deps chromium webkit
SERYN_E2E=1 \
SERYN_E2E_URL=http://127.0.0.1:18080 \
npm run test:e2e
```

Provision the disposable container through environment variables. Browser artifacts are written under the ignored `tests/web/artifacts/` directory. Automated WebKit does not replace physical Safari and iPhone validation.
