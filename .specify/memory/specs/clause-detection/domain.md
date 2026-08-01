# Domain Model

**Feature:** Clause Detection

**Version:** 1.0.0

**Status:** Draft

---

# Purpose

Define the domain concepts, invariants, and state transitions for clause detection.

This document describes business behavior only and excludes implementation details.

---

# Aggregate Root

## Document

The Document aggregate remains the consistency boundary for ingestion and downstream processing.

Clause detection enriches a Document revision with ordered clause units derived from NormalizedText.

---

# Entities

## Clause

Represents one detected clause within a specific Document revision.

Properties:

- ClauseId
- Sequence
- ClauseNumberLabel (optional)
- ClauseText
- SpanStart
- SpanEnd

Responsibilities:

- Preserve one clause unit in reading order.
- Retain numbering label when available.
- Preserve source span for traceability.

---

# Value Objects

## ClauseId

Deterministic identifier for a clause within a document revision.

## ClauseNumberLabel

Represents the original numbering marker when present.

Examples:

- 1
- 1.1
- A
- IV

## ClauseText

Immutable textual content of the clause.

## ClauseSpan

Character-range reference in NormalizedText.

Properties:

- Start
- End

Invariant:

- Start >= 0
- End > Start

---

# Aggregate State Changes

Before detection:

- Status = Normalized
- Clauses = empty

After successful detection:

- Status = ClausesDetected
- Clauses = ordered, non-empty

After failure:

- Status = Failed
- FailureReason = populated

---

# Business Invariants

## INV-001

Clause detection may run only when NormalizedText exists.

## INV-002

Clause sequence values are unique and strictly increasing within a document revision.

## INV-003

ClauseId values are unique within a document revision.

## INV-004

Clause detection must not mutate NormalizedText.

## INV-005

Detected clauses preserve document reading order.

## INV-006

A document in Failed status cannot transition directly to ClausesDetected without explicit reprocessing flow.

## INV-007

Clause results are tenant-scoped and cannot cross tenant boundaries.

---

# Domain Behaviors

## DetectClauses()

Detects and stores ordered clauses for the current document revision.

Preconditions:

- Document exists.
- Tenant ownership is validated.
- Status = Normalized.
- NormalizedText is present.

Postconditions on success:

- Clauses assigned.
- Status = ClausesDetected.
- ClauseDetectionCompleted published.

Postconditions on failure:

- Status = Failed.
- FailureReason assigned.
- ClauseDetectionFailed published.

## EnsureIdempotentClauseDetection()

Prevents duplicate clause records when clause detection is requested repeatedly for the same document revision.

---

# Domain Events

## ClauseDetectionCompleted

Published when detection succeeds.

Properties:

- DocumentId
- TenantId
- RevisionNumber
- ClauseCount
- CorrelationId
- OccurredAt
- Version

## ClauseDetectionFailed

Published when detection fails.

Properties:

- DocumentId
- TenantId
- RevisionNumber
- FailureReason
- CorrelationId
- OccurredAt
- Version

---

# Relationships

Document
    |
    +-- Clause (0..*)

Clause is contained within the Document aggregate boundary.

---

# State Transitions

Normalized
    |
    v
ClausesDetected

Failure path:

Normalized
    |
    v
Failed

---

# Consistency Rules

- Clause detection outcomes are stored per document revision.
- Re-execution for the same revision must not create duplicate clauses.
- Events are emitted only after persistence of the corresponding state.
- Tenant context is required for all clause detection operations.

---

# Future Evolution

Potential future extensions:

- Hierarchical clause structure (parent-child)
- Confidence scoring per clause boundary
- Language-aware boundary detection rules
- Partial re-detection for changed revision segments
