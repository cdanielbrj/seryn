# ADR-003 — Phase 2 record storage and CRUD

Status: accepted and implemented for the record CRUD increment.

## Decision

Seryn persists one original row for each expected income or expense. The stored amount is a positive integer number of cents per occurrence. Direction supplies the future projection sign. No installment rows, derived end date, series total, payment state, or projected occurrence is persisted.

The current record contract contains a required name, direction, type, payment method, per-occurrence amount, start year/month, and recurrence. Category is optional and may reference either a root category or one direct subcategory. The supported recurrences are once, monthly, quarterly, semiannual, and annual.

Occurrence mode is represented canonically by the nullable occurrence count instead of a second persisted flag:

- once always stores count `1`, regardless of redundant client values;
- a defined recurring record stores a count of at least `2`;
- an indefinite recurring record stores `null`.

The public JSON contract remains in Brazilian Portuguese. SQLite and C# use technical English names. Start precision is preserved as separate year and month integers, so the application does not invent a financial day.

For a defined recurring record, clients may submit either a per-occurrence amount or a series total. A submitted total is divided by the occurrence count and rounded to the nearest cent, with half-cent ties rounded upward. Only the resulting per-occurrence amount is persisted; the input mode and submitted total are UI/application conveniences rather than additional financial sources of truth. Editing an existing record therefore starts from its canonical per-occurrence amount.

Types and payment methods are required foreign keys with restricted deletion. An in-use type or payment method cannot be deleted. Category is optional: deleting an in-use subcategory moves its records to the direct parent, while deleting an in-use root category clears the category reference. A root category with children must still be emptied or reorganized before deletion. No classification operation deletes a financial record.

All UI and HTTP writes use the same scoped C# application service. The API validates the complete post-update state and exposes authenticated CRUD under `/api/v1/records`. Mutations continue to use the existing antiforgery flow.

## Deliberate limits

This increment does not calculate projections, populate dashboard totals, add import/export fields, or create inline classifications from the record form. When no type or payment method exists, the form links to their existing management pages instead of accepting an invalid record.
