---
name: seryn-change-validation
description: Validate a Seryn code, build, or configuration change before completion or commit, selecting the relevant frontend, backend, Docker, and reference checks and reporting any untested scope.
---

# Seryn change validation

Read `docs/development/testing.md` and inspect the actual diff before choosing checks.

Validate in proportion to risk:

- Frontend behavior or structure: run lint, unit tests, TypeScript, and the production build.
- Backend behavior, persistence, security, or contracts: run Release build, tests, formatting verification, and the pending-model check when EF models may have changed.
- Docker or deployment behavior: build the affected test stage and final image as appropriate.
- Cross-stack or release-facing changes: validate both stacks.

Use existing package locks and test stages. Do not install or upgrade dependencies merely to validate a change.

Run `git diff --check`. Search for stale imports, paths, and references when files moved. For financial behavior, include the canonical numeric cases required by the product specification.

Do not run destructive or end-to-end tests against the shared `seryn` Compose project or its persistent volume. Use a disposable project when those tests are necessary.

Report the checks that passed, checks that failed and why, checks intentionally not run, and any remaining manual or environment-specific validation. Do not claim completion while a required check is failing.
