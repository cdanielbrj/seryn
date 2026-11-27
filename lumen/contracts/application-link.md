# Application Link contract

An Application Link lets an external consumer call host capabilities through an authenticated, revocable identity. The direction is external consumer to host, regardless of whether the consumer is an AI agent, another first-party application, or conventional automation.

An Application Link defines:

- credential format and validation;
- consumer name and stable identity;
- granted capabilities or scopes;
- expiration and revocation;
- rate-limit and idempotency partitioning;
- mapping into host authorization policies.

It does not own interactive user login, host recovery, or provider credentials. The host's security infrastructure may compose interactive and Application Link schemes while keeping their implementations independent.

Seryn Link uses revocable `seryn_pat_` Bearer credentials and `data:read`, `data:write`, and `data:delete` capabilities. Those names and policies are Seryn-specific, not mandatory Lumen vocabulary.
