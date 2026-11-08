# Theme architecture

Themes are concrete visual implementations of the semantic contracts exposed by `src/web/app`. Application TSX defines structure, state, and interaction; themes define presentation.

The initial implementation is `main`:

```text
main/
├── tokens/
├── foundations/
├── layouts/
├── components/
├── features/
└── index.css
```

- `tokens` defines common values and light/dark color modes.
- `foundations` defines reset, typography, and accessibility behavior.
- `layouts` styles persistent application and authentication composition.
- `components` implements reusable component contracts.
- `features` styles product-specific composition without reimplementing component foundations.

`index.css` is the only theme entry point and controls cascade layer order. Theme identity (`data-theme`) and color scheme (`data-color-scheme`) are independent.

CSS filenames follow the contract they implement. Prefer stable semantic selectors and explicit states. Do not move behavior into CSS or import theme files from components. A future theme must implement the same structural contract under a sibling directory.

See [ADR-007](../../../docs/architecture/adr/ADR-007-FRONTEND-COMPONENT-AND-THEME-ARCHITECTURE.md) and the [application guide](../app/README.md).
