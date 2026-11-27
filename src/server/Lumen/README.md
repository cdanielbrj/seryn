# Seryn Lumen backend

This directory is Seryn's backend implementation of the portable [Lumen architecture](../../../lumen/README.md).

- `Core` contains provider-neutral coordination contracts and behavior.
- `OpticalLink` contains outbound provider connections and adapters.
- `Persona` contains Seryn's host-specific identity and response policy.
- `SerynLink` contains Seryn's inbound Application Link.
- `Tools` adapts Seryn application services to the Lumen tool contract and is intentionally host-specific.

`LumenModule` is the host composition boundary. Provider, persistence, ASP.NET, and Seryn module dependencies must not move into Core.
