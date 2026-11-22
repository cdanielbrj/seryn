# API integrations

Seryn exposes its versioned contract at `/api/v1` on the same HTTPS origin as the web application. The authenticated OpenAPI document is available at `/api/v1/openapi/v1.json`.

Create and revoke integration tokens in **Settings → Integration tokens**. Copy a new secret immediately; Seryn shows it only once and stores only its hash. Prefer a 90-day expiration and the minimum scopes required by the client.

## Authentication and scopes

Send the token in every request:

```http
Authorization: Bearer seryn_pat_<secret>
```

Available scopes are `data:read`, `data:write`, and `data:delete`. Delete requires write. Browser sessions satisfy data policies for the first-party UI, while integration tokens must carry the explicit scope.

Tokens never authorize:

- workbook export, preview, import, or complete replacement;
- token creation, listing, or revocation;
- backup or restore operations.

Those operations require the interactive administrator session and CSRF protection. Existing classification rules still apply: a token cannot delete an in-use type or payment method, nor a category whose children must first be reorganized.

## Safe retries

Bearer-authenticated POST requests can include a client-generated key:

```http
Idempotency-Key: 5e72c030-50f4-4cf9-90e4-af14f124b565
```

For 24 hours, an identical retry returns the original status, body, content type, and relevant representation/navigation headers (`Location`, `ETag`, `Last-Modified`, `Cache-Control`, `Content-Language`, `Link`, `Deprecation`, and `Sunset`) with `Idempotency-Replayed: true`. Authentication, cookie, connection-specific, rate-limit, and other transient headers are deliberately not replayed. Responses below 500 are retained; a 5xx response can be retried with the same key. Reusing the key with a different method, path, query, or body returns `409 Conflict`. Keys must contain 8–128 non-control characters. Use a new unpredictable key for each logical creation.

When both a session cookie and an `Authorization: Bearer` header are sent to a data endpoint, Bearer authentication takes precedence. An invalid or insufficiently scoped token is never allowed to fall back to the cookie. Endpoints explicitly restricted to `admin-session` authenticate the cookie only and remain unavailable to Bearer tokens; browser clients should nevertheless omit the Authorization header.

## Network boundary

Seryn does not publish itself or enable cross-origin browser access. The administrator controls reachability through the deployment URL and reverse proxy. Prefer VPN, Tailscale, or another private network. When remote access is intentionally enabled, use HTTPS and protect the token as a password-equivalent secret.

## Compatibility

Within `/api/v1`, existing fields keep their names and meanings. Responses may gain fields. Removed or renamed fields, newly required inputs, and incompatible semantic changes require a new API version. The current v1 field identifiers remain stable even if the product UI gains another language.
