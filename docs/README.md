# Seryn documentation

This directory is the canonical documentation hub for Seryn.

## Sections

- [Product specification](product/SERYN_SPEC.md): product scope, financial rules, public contracts, and acceptance criteria.
- [Architecture](architecture/README.md): current architecture and the decision record index.
- [Design](design/README.md): visual source-of-truth rules and reference prototypes.
- [Deployment](deployment/README.md): Docker, configuration, reverse proxy, backup, and Unraid operations.
- [Development](development/README.md): local environment, testing, and contribution workflow.
- [Validation](validation/README.md): dated evidence for completed validation milestones.

## Documentation policy

- The product specification defines what Seryn must do.
- ADRs explain why a durable technical decision was made; they are not operational tutorials.
- Current operating instructions belong in deployment or development documentation.
- Design artifacts communicate approved composition and visual intent, never financial algorithms.
- Validation records are historical evidence and are not updated to describe later phases.
- READMEs close to source code explain how to work within that area and link back to canonical decisions.
- Agent workflows live under `.agents/skills`; they reference this documentation rather than duplicating it.

Repository documentation is written in English. Product UI and public contracts remain in Brazilian Portuguese where required by the specification.
