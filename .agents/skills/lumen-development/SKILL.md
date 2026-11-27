---
name: lumen-development
description: Implement, extend, port, or review the Lumen architecture, including Core coordination, Optical Links, Application Links, host tool catalogs, and Lumen public contracts. Use for Lumen work, not for ordinary host features that merely expose an existing tool.
---

# Lumen development

Treat `lumen/` at the repository root as the canonical portable specification. Do not duplicate its architecture or security rules in this skill.

## Route the task

Read `../../../lumen/README.md` first, then load only the relevant canonical sources:

- Core coordination or architecture boundaries: `../../../lumen/ARCHITECTURE.md`, `../../../lumen/SECURITY.md`, and `../../../lumen/contracts/messages.md`.
- Host tools: `../../../lumen/ARCHITECTURE.md`, `../../../lumen/SECURITY.md`, and `../../../lumen/contracts/tools.md`.
- A model provider or connection: `../../../lumen/SECURITY.md` and `../../../lumen/contracts/optical-link.md`.
- An inbound host integration: `../../../lumen/SECURITY.md` and `../../../lumen/contracts/application-link.md`.
- Porting Lumen to another host or reviewing conformance: read every canonical Lumen document and `references/adoption-checklist.md`.

Also read the target host's contributor instructions, product specification, and nearest source README before changing its implementation.

## Work within the boundary

- Keep Core provider-neutral and host-neutral.
- Put provider protocol and credentials in an Optical Link.
- Put inbound consumer credentials and capability identity in an Application Link.
- Adapt tools over existing host application services; never bypass host validation or persistence boundaries.
- Keep interactive host authentication separate from Application Link authentication, composing them only in host security infrastructure.
- Preserve public contracts unless the user explicitly authorizes a breaking revision.
- Do not extract shared packages before multiple real hosts demonstrate stable common semantics.

Update the canonical Lumen specification when a change modifies the reusable pattern. Update only host documentation when the change is host-specific.

## Validate

Run the target host's relevant build, test, formatting, persistence, security, and contract checks. For new links, verify credential non-disclosure, restart persistence where applicable, bounded failures, and the intended authorization direction. Report which behavior is portable Lumen and which remains host-specific.
