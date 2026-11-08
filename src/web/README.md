# Frontend package

`src/web` owns the React/TypeScript/Vite application, its concrete themes, static browser assets, tool configuration, and the single Node dependency installation used by frontend source and tests.

## Structure

```text
src/web/
├── app/        semantic structure, behavior, and product modules
├── themes/     concrete visual implementations of app contracts
├── static/     files copied without transformation
├── index.html  Vite HTML entry point
└── package.json
```

Generated `dist`, TypeScript build metadata, and `node_modules` are not source and remain ignored.

## Boundaries

- Application TSX and TypeScript live under `app`; see the [application architecture](app/README.md).
- Theme CSS lives under `themes`; see the [theme architecture](themes/README.md).
- Static manifests, pre-paint scripts, and icons that require stable public paths live under `static`.
- Frontend test source lives outside this package under [`tests/web`](../../tests/README.md), but uses this package's dependency installation and tool configuration.

## Common commands

Run from `src/web`:

```sh
npm ci
npm run dev
npm run lint
npm test
npm run build
```

The complete testing strategy, Docker stages, and E2E isolation rules live in [development testing documentation](../../docs/development/testing.md).
