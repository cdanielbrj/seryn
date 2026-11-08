# ADR-007: Frontend Component and Theme Architecture

## Status

Accepted.

## Context

The frontend must allow its visual identity to change without duplicating application behavior or coupling React components to one concrete stylesheet. The previous stylesheet combined tokens, foundations, layouts, reusable components, feature composition, responsive behavior, and color modes in one file.

## Decision

Treat `src/web/app` as the structural and behavioral contract and `src/web/themes` as concrete visual implementations of that contract.

Application code follows the roles and dependency direction defined by [ADR-008](ADR-008-FRONTEND-APPLICATION-ARCHITECTURE.md). That organization distinguishes reusable React contracts, persistent layout, product modules, and technical infrastructure without changing the theme boundary.

React components expose stable semantic class names, slots, states, and data attributes. They do not import concrete theme CSS. A theme implements those selectors and may change layout, spacing, typography, color, and responsive presentation without changing application behavior.

The initial implementation is `themes/main`. Its single entry point, `index.css`, uses cascade layers and imports these responsibilities in a defined order:

- `tokens` contains common custom properties and light/dark color values;
- `foundations` contains reset, typography, and accessibility rules;
- `layouts` contains application-wide composition such as the shell, navigation, and authentication layout;
- `components` implements reusable visual contracts such as panels, buttons, forms, dialogs, and controls;
- `features` contains only composition and presentation specific to a product capability.

Theme identity and color scheme are independent. The root element uses `data-theme="main"` for the selected visual implementation and `data-color-scheme="light|dark"` for its color mode. The pre-paint script and React theme control maintain both attributes while preserving the existing local preference.

CSS files are named after the contract they implement. Feature styles may compose reusable components but must not reimplement their base visual rules. Additional themes must implement the same stable selector contract under a sibling directory and provide their own entry point.

## Consequences

- TSX structure and behavior can remain stable while a theme changes the complete visual presentation.
- Light and dark are modes of a theme rather than separate themes.
- Reusable component rules have one implementation per theme instead of being duplicated across feature stylesheets.
- Global selectors are an intentional internal API and require the same care as component props when renamed.
- Cascade order is explicit rather than dependent on an accidental monolithic file order.
- A future runtime theme selector will need to load or activate the chosen theme entry point without changing feature components.
