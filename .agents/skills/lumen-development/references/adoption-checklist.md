# Lumen adoption checklist

Use this checklist when porting Lumen to a new host or conducting a full conformance review.

## Host definition

- Name the host-specific Application Link.
- Identify the existing application services that may become tools.
- Define interactive identity separately from programmatic consumer identity.
- Identify which data may leave the host through an Optical Link.

## Core

- Core imports only normalized contracts.
- Message context, tool rounds, calls, sizes, concurrency, and timeouts are bounded.
- Provider responses and tool correlation identifiers are validated.
- Host persona and policy remain host inputs rather than provider behavior.

## Optical Link

- Each provider has a concrete adapter outside Core.
- Credentials are server-side, protected at rest, masked after creation, and absent from logs and exports.
- Endpoints, redirects, response size, timeout, and error translation are controlled.
- Connection selection does not leak provider-specific concepts into Core.

## Application Link

- Credentials are high entropy, revocable, and independently expirable where applicable.
- Stored credential material follows the minimum recoverability required by its direction.
- Consumer capabilities map into host authorization policies.
- Rate limits, audit identity, and idempotency use the Application Link identity.
- Credential administration requires the host's interactive administrative policy.

## Tools

- The catalog is an explicit allowlist.
- Tools call existing host services and validation.
- Exact domain calculations remain in the host.
- Arguments, results, sources, and truncation are bounded and explicit.
- Mutating tools, if any, have a policy distinct from read-only tools.

## Delivery

- Public contract changes are intentional and tested.
- Persistence changes are migrated and restart-safe.
- Canonical Lumen documents describe reusable decisions; host documents describe host-specific integration.
- The implementation states what is experimental and avoids unsupported compatibility claims.
