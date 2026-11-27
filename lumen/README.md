# Lumen

Lumen is an experimental architecture for connecting an application's capabilities, AI models, and external consumers without making any one provider or host application the center of the design.

This directory is the portable specification. It describes the pattern that can be carried to another project. It is not a distributable library, a hosted service, or a claim that the current Seryn implementation is already host-independent.

## Vocabulary

- **Lumen Core** coordinates conversations, model requests, tool calls, limits, and host policies.
- **Optical Link** connects a host application to an external or local model provider.
- **Application Link** lets an external agent or application invoke capabilities exposed by a host.
- **Seryn Link** is Seryn's Application Link implementation.
- **Host tools** adapt real application services to Lumen tool contracts. They remain governed by the host's validation and authorization rules.

```text
external agent ── Application Link ──▶ host tools
                                          ▲
                                          │ Lumen Core
                                          ▼
host application ── Optical Link ──▶ model provider
```

## Status

Lumen is being discovered through Seryn, its first reference implementation. Contracts may change while a second host has not yet validated which abstractions are genuinely portable. Do not publish packages or promise cross-project binary compatibility from this specification yet.

The current implementations live in:

- [Seryn backend](../src/server/Lumen/)
- [Seryn frontend](../src/web/app/modules/lumen/)

## Adoption path

1. Read [the architecture](ARCHITECTURE.md) and [security model](SECURITY.md).
2. Define the new host's Application Link and identity model.
3. Implement a host tool catalog over existing application services.
4. Add only the Optical Links the host actually needs.
5. Keep provider secrets and Application Link credentials under separate lifecycles.
6. Validate the implementation against the contracts in [`contracts/`](contracts/README.md).
7. Feed proven differences back into this specification before extracting shared packages.

Use the repository's `lumen-development` skill when implementing, extending, or reviewing this pattern.
