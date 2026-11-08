# ADR-004: Dashboard projections are calculated from the record source of truth

## Status

Accepted

## Context

The dashboard must present period totals, a projection chart, root-category expense distribution, and the highest-impact records without storing virtual future rows or implying a bank balance.

## Decision

- `GET /api/v1/projections?period=month|semester|year&reference=YYYY-MM` is the authenticated dashboard read model.
- A pure C# recurrence module expands original records only within the requested finite month range. It is used by the application service rather than by EF queries or the React client.
- Summary cards aggregate the selected calendar month, semester, or year. The result is always income minus expenses.
- Semester and year charts use the selected period. A monthly dashboard uses a seven-month contextual window centered on the selected month; the selected month is marked in the response.
- Every chart cumulative series starts at zero at the beginning of its displayed range. It is not, and is never labelled as, an account balance.
- Expense categories roll a child category into its root parent. Unclassified expenses are grouped as `Sem categoria`.
- Highest income and expense lists aggregate virtual occurrences by original record inside the selected period, then return the top three per direction.

## Consequences

No projections, category totals, record totals, or chart values are persisted. Editing or deleting an original record intentionally changes all derived historical and future projections. The monthly contextual chart is explicitly described by its displayed start and end months, so its final cumulative value is not presented as the single-month summary result.
