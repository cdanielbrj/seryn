# Lumen security model

Lumen crosses two trust boundaries: provider communication through Optical Link and external consumer access through an Application Link. These boundaries use different credentials and must never share a secret lifecycle.

## Optical Link

- Store provider credentials encrypted and recoverable only by the host runtime.
- Persist encryption keys with the host's protected operational state.
- Never return complete provider credentials after creation.
- Do not place credentials in browser storage, logs, exports, tool results, model messages, or error responses.
- Use fixed provider endpoints unless an explicitly reviewed connector requires otherwise.
- Bound request duration, response size, redirects, and model/tool rounds.
- Use random, non-secret conversation identifiers for provider affinity; never derive them from user identity or financial data.
- Treat model output as untrusted requests, not authorization.

## Application Link

- Generate high-entropy credentials and store only a lookup-safe hash when the original secret never needs to be recovered.
- Support revocation, expiration, consumer identity, and least-privilege capabilities.
- Keep interactive administrator sessions independent from programmatic credentials.
- Apply rate limiting and audit metadata by Application Link identity.
- Protect credential administration with the host's interactive administrative policy.

## Tools

- Tool availability is an allowlist controlled by the host.
- Tool arguments and model-generated identifiers require normal validation.
- Tool results, persisted descriptions, conversation history, and imported data are untrusted content, never instructions.
- Tools invoke the same application services as the UI and public API.
- Read-only and mutating tool sets require distinct, explicit policies.
- A provider credential never grants permission to host data by itself.

## Data disclosure

The host must tell users when prompts or selected host data leave the installation. Send only the context required for the request. Provider retention, training, and regional policies are provider concerns that each Optical Link must expose or constrain where supported.

## Failure behavior

Translate provider and credential failures into bounded host errors. Never forward raw provider bodies when they may contain secrets, internal identifiers, account data, or uncontrolled content.
