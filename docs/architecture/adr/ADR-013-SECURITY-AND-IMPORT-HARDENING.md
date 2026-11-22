# ADR-013 — Deployment, session, and workbook hardening

Status: accepted and implemented.

## Context

Seryn supports local HTTP for loopback development, trusted reverse proxies for remote HTTPS, persistent administrator sessions, and complete workbook replacement. These capabilities need fail-closed boundaries against accidental plain-HTTP publication, forged forwarding headers, stale sessions, resource-exhaustion workbooks, and confirmation of a different workbook than the one previewed.

## Decision

Plain HTTP is allowed automatically only when the Compose-published address is loopback. A non-loopback published address requires observed HTTPS or the explicit `SERYN_ALLOW_INSECURE_REMOTE=true` operator override. The application continues to trust only exact configured proxy addresses, one forwarding hop, and allowed hosts.

Administrator sessions persist their creation, last-use, and expiry timestamps. The current session can list active sessions and revoke another session or all other sessions. Session administration remains unavailable to Bearer tokens. Expired sessions are rejected and removed opportunistically.

Workbook import performs ZIP/OpenXML preflight before ClosedXML allocation. It limits compressed and expanded size, entries, worksheets, used rows and cells, entity rows, individual parts, and compression ratio; rejects invalid internal paths, duplicate parts, malformed XML, DTDs, and external relationships; and continues to ignore bounded unknown sheets, internal VBA parts, custom XML, hidden sheets, defined names, and presentation details without assigning them product semantics.

A successful preview creates short-lived, single-use authorizations for merge and, when the workbook is complete, replacement. Each authorization is bound to the administrator session, exact workbook hash, intended mode, and a hash of the current user-data state. Commit revalidates the workbook and state, requires the matching preview identifier, and consumes it in the same transaction as the import. Any failure rolls back deletion, creation, and preview consumption together.

## Consequences

Local Compose remains available at `127.0.0.1` without TLS. Remote HTTP requires a visible operator decision. A reviewed workbook cannot be swapped before commit, and concurrent changes require a new preview. Malicious or unexpectedly complex packages are rejected before general workbook parsing. Workbook replacement remains administrator-only and atomic.
