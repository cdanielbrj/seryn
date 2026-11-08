---
name: seryn-local-deployment
description: Build, update, or verify the shared local Seryn Compose instance at localhost:8080 while preserving its persistent volume, credentials, data, and required running state.
---

# Seryn local deployment

Read `docs/deployment/docker.md` before operating the shared instance.

The shared target is fixed:

- Compose project: `seryn`;
- URL: `http://localhost:8080`;
- persistent volume: `seryn_seryn-data`.

Update it from the repository root with:

```sh
docker compose -p seryn up --build -d --wait
```

Container recreation is allowed. Never delete, replace, or reset the persistent volume, credentials, or stored data. Never run `down -v`. Do not stop the shared instance when the task finishes.

After an update, verify the Compose health result and make a non-destructive HTTP request to localhost. If the build, startup, healthcheck, or request fails, inspect bounded logs and report the cause. Do not respond to a failure by discarding data or broadening the deployment scope.

Destructive and end-to-end tests must use a separate Compose project and disposable volume. Updating the shared instance does not authorize testing that mutates or erases its existing data.
