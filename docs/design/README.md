# Design

The files under `prototypes/` are the approved visual references for Seryn. The interactive HTML prototype is the primary reference, supported by desktop and mobile screenshots in light and dark modes.

The v2 prototype remains the visual source for the dashboard and persistent shell. The later Análises area is an approved responsive extension defined by the product specification; it reuses the established shell, tokens, panels, period controls, and accessibility contracts without changing the dashboard prototype.

Objetivos is a second approved responsive extension. It reuses the same shell, panels, form controls, semantic badges, themes, and mobile accessibility contracts while keeping objective values visually and semantically separate from projected financial results.

## Source-of-truth order

1. The [product specification](../product/SERYN_SPEC.md) defines behavior, financial semantics, public language, and acceptance criteria.
2. The prototype defines approved composition and visual intent where the specification refers to it.
3. React code defines semantic structure and interaction contracts.
4. Themes implement those contracts as concrete visual systems.

Prototype values and calculations are fictitious. They must never be copied as financial rules or expected numeric results.

## Artifacts

- [`seryn_mock_v2.html`](prototypes/seryn_mock_v2.html): interactive desktop/mobile concept and primary visual reference.
- `seryn_desktop_light.png` and `seryn_desktop_dark.png`: desktop references.
- `seryn_mobile_light.png`, `seryn_mobile_dark.png`, and `seryn_mobile_comparacao.png`: mobile references.

Update a prototype when product-level composition or visual direction changes. Update `src/web/themes` when implementing a visual contract. An implementation detail that does not alter approved product behavior or visual direction does not require a prototype revision.
