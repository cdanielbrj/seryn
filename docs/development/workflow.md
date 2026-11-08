# Documentation and delivery workflow

## Sources of truth

- Update the product specification when product scope, financial behavior, public contracts, or acceptance criteria change.
- Add an ADR for a durable technical decision; do not use an ADR as a changelog.
- Update deployment or development documentation when current commands, configuration, or operating procedures change.
- Update design prototypes when approved composition or visual direction changes.
- Add a validation record only when preserving evidence for a meaningful milestone or release.
- Update a local source README when the way contributors work within that area changes.

## Delivery

Keep changes small and testable. Before completion, validate the affected stack, check documentation references, summarize what works, explain how it was tested, and identify anything intentionally left out.

All repository content is written in English. Brazilian Portuguese remains the language of the product interface and public contracts where specified.

Agent procedures under `.agents/skills/` automate repeatable workflows. They must reference canonical documentation instead of copying it.
