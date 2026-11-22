# ADR-012 — Scoped API access for integrations

Status: accepted and implemented for the API-readiness increment.

## Context

Seryn's browser uses an interactive administrator session with a SameSite cookie and CSRF protection. Local agents, MCP servers, command-line clients, and future external integrations must not store the administrator password or emulate that browser session. They also need retries that cannot silently duplicate writes.

Data portability and host recovery are deliberate administrator actions. Possession of an integration credential must never permit workbook export/import, complete replacement, backup, restore, or credential administration.

## Decision

Seryn issues personal integration tokens with the `seryn_pat_` prefix. The complete secret is displayed once; SQLite stores only its SHA-256 hash, a non-secret display prefix, name, immutable scopes, timestamps, and optional expiration. High-entropy random secrets make a fast cryptographic hash appropriate for lookup; administrator passwords continue to use the deliberately expensive password hasher.

Bearer authentication uses `Authorization: Bearer <token>` over the same HTTPS origin administered by the installation owner. Network reachability is a deployment concern; VPN or another private network is preferred, but it never replaces token authentication. Seryn does not enable permissive CORS or expose itself publicly.

The scopes are:

- `data:read` for records, objectives, classifications, projections, and analyses;
- `data:write` for creating and editing individual resources;
- `data:delete` for deleting individual records, objectives, and classifications, subject to the existing referential rules; it requires `data:write`.

Tokens cannot call portability or token-administration endpoints. Future backup and restore endpoints will also require an interactive administrator session. Token creation, listing, and revocation require that session and CSRF. Scopes are immutable, active tokens are limited to twenty, expiration is optional, and revocation is immediate.

Bearer-authenticated POST requests may send `Idempotency-Key`. Seryn binds the key to the token, path, query, method, and request body for 24 hours. An identical retry replays the stored non-server-error status, body, content type, and an explicit safe list of representation/navigation headers; reuse with a different request returns conflict. Synchronization is scoped to the token and key, so unrelated integration requests do not block each other. If cookie and Bearer credentials are both present on data endpoints, Bearer takes precedence without cookie fallback; policies explicitly restricted to the administrator session authenticate only the cookie.

`/api/v1` preserves the names and meanings of existing fields. Additive response fields are allowed; removals, renamed fields, newly required inputs, or semantic changes require a new API version. The v1 identifiers remain in their current Brazilian Portuguese form. A future API version may choose language-neutral identifiers independently from UI localization.

## Consequences

Integrations can use least-privilege credentials without receiving the administrator password. A compromised token cannot export or replace the complete portable dataset, manage other tokens, or perform host recovery. The OpenAPI document describes Bearer authentication and stable operation identifiers, and contract tests protect its integration-facing metadata.

The first integration should prefer `data:read`. Granting write or delete scopes is an explicit administrator action. Confirmation inside an AI client remains that client's responsibility; the API cannot verify a client's assertion that a human approved an action.
