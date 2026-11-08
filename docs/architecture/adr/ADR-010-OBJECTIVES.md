# ADR-010 — Objectives are independent planning artifacts

Status: accepted.

## Context

Users need to keep possible acquisitions at different levels of interest and relative priority before deciding whether they belong in the financial record model. These items must not silently affect expected income, expenses, or projections.

## Decision

Seryn adds an `Objectives` module and an `objectives` SQLite table. An objective stores a required name, positive estimated value in integer cents, interest classification, priority from `-3` through `+3`, automatic inclusion timestamp, update timestamp, and optional link, notes, and desired year/month.

Interest is stored as an enum with `Listed`, `Considered`, and `Desired` values and exposed in the Brazilian Portuguese contract as `listado`, `considerado`, and `desejado`. It is a freely editable classification rather than a lifecycle. Priority is an independent user-defined ordering signal and defaults to zero in the UI.

Authenticated CRUD is exposed at `/api/v1/objectives`. Backend application validation is authoritative. Objectives have no foreign key to financial records, and objective writes do not invoke the recurrence or projection modules.

The frontend owns a separate vertical `objectives` module with responsive list and form routes. Default ordering combines interest, priority, and inclusion date; search, filtering, and alternative sorting are presentation concerns over the returned objective collection.

## Consequences

Objectives survive upgrades and container replacement through the existing SQLite volume while remaining semantically isolated from expected financial values. Future simulations or explicit record conversion require separate product decisions and must not be inferred from this schema.
