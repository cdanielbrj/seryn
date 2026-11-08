---
name: seryn-docs-sync
description: Audit and update Seryn documentation after product, architecture, design, deployment, workflow, path, or public-contract changes while preserving the repository's documentation hierarchy.
---

# Seryn documentation synchronization

Read `docs/README.md` and `docs/development/workflow.md`, then inspect the change or decision that triggered the update.

Update the narrowest canonical source:

- product behavior, financial semantics, public contracts, or acceptance criteria: `docs/product/SERYN_SPEC.md`;
- durable technical decisions: a sequential ADR under `docs/architecture/adr/` and its index;
- current deployment or configuration procedures: `docs/deployment/`;
- current development or testing procedures: `docs/development/`;
- approved visual direction: `docs/design/`;
- milestone evidence: `docs/validation/`;
- area-specific contributor guidance: the nearest source README.

Keep the root README concise. Keep `AGENTS.md` limited to rules that must always be loaded. Keep repeatable procedures in `.agents/skills/`.

Do not duplicate canonical content across documents. Prefer a short explanation and a relative link. Do not rewrite historical validation records to describe later work, and do not create an ADR for routine implementation details.

Search the repository for old paths, filenames, phase claims, commands, and cross-references. Ensure relative links resolve from the document that contains them. Repository content is English; Brazilian Portuguese remains only where required for product UI or public contracts.

Finish with a summary of updated sources and any documentation that still requires product-owner confirmation.
