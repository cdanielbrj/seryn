# ADR-009 — Projection analyses are derived read models

Status: accepted.

## Context

The dashboard summarizes one period, but users also need to investigate how planned income and expenses are distributed across categories, payment methods, and types. The analysis must remain consistent with Seryn's recurrence rules and must not become a second financial source of truth.

## Decision

The Projections module owns an authenticated `GET /api/v1/analyses` read model. It accepts a finite month, semester, or year, a direction filter, one primary dimension, and an optional different secondary dimension.

The application service loads original records and their classifications, invokes the existing pure recurrence calculator for the selected range, and aggregates the resulting virtual occurrences in memory. It returns summary totals, segment totals, percentage shares, distinct original-record counts, occurrence counts, optional crossings, and record-level composition. For finite recurring records, record composition also derives the total installment count and the number projected through the selected period end. No occurrence, aggregate, or installment progress is persisted.

Categories use their root category for analysis, matching dashboard distribution. Records without a category use `Sem categoria`. When both directions are selected, percentages use projected volume (`income + expenses`) as their denominator. A single-direction analysis uses that direction's total. Result remains projected income minus projected expenses under the filter.

The frontend implements Análises as its own vertical product module and adds it to desktop and mobile primary navigation. Segment details link back to the original record editor. The existing dashboard composition and endpoint remain unchanged.

## Consequences

Dashboard and analyses use the same recurrence semantics without storing duplicate financial facts. Editing an original record or classification intentionally changes later analyses. The response may grow with the number of records because the first increment returns record composition for every segment; pagination or narrower detail endpoints should be introduced only when measured data volume requires them.

This decision does not implement plans, wishlist items, affordability claims, balances, card limits, or payment execution.
