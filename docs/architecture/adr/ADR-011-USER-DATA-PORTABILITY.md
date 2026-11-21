# ADR-011 — User-data portability uses canonical workbooks

Status: accepted.

## Context

Seryn needs a portable, user-readable representation for migration and recovery of information entered through the application. This operation is distinct from a host backup: authentication, sessions, keys, deployment configuration, and operational access data must remain local to the installation.

CSV cannot represent records, objectives, hierarchical categories, and unused classifications without multiple partial contracts. Internal UUIDs would also expose persistence details without safely identifying edits once a workbook is changed outside Seryn.

## Decision

Seryn exports complete user data as one `.xlsx` workbook with separate `Registros`, `Objetivos`, `Tipos`, `Categorias`, and `Pagamentos` sheets. Import accepts `.xlsx` and `.xlsm`; macro-enabled workbooks are treated only as OpenXML containers, and macros are never executed or persisted. CSV and legacy XLS are not portability formats.

The workbook contains readable values and relationships rather than internal database IDs. Exact semantic equivalents are detected from their complete values. Import never guesses that an edited row replaces an existing row.

Recognized sheets require the canonical headers and valid values. Formulas in Seryn columns are rejected. Unknown sheets, styles, macros, and other external content have no product semantics.

Import exposes two explicit operations after preview:

- merge creates new items and retains equivalent or unrelated current data;
- complete replacement requires all five canonical sheets and a separate destructive confirmation, deletes only user-entered financial data, objectives, and classifications, and recreates the workbook content.

Every confirmed import runs through application validation in one database transaction. Failure rolls back both deletion and creation. Authentication and host state are outside the transaction's deletion scope.

Export and import have separate frontend destinations and independently callable authenticated API operations under `/api/v1/portability`.

## Consequences

One format represents the complete user-data graph and supports local testing, installation migration, and recovery without coupling installations through UUIDs. Merge is intentionally conservative; users choose complete replacement when edited workbook data must become authoritative.

The workbook is not a complete host backup and cannot restore credentials, sessions, keys, or deployment state. Supporting targeted workbook updates, CSV, legacy XLS, or finer host administration exports requires a separate product decision.
