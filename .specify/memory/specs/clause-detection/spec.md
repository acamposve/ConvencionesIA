# Feature Specification

**Feature:** Clause Detection

**Status:** Draft

**Version:** 1.0.0

---

# Vision

Enable the platform to identify and segment contractual clauses from normalized document text so downstream capabilities can reason over clause-level units instead of raw document text.

---

# Goal

Detect clause boundaries and produce an ordered, deterministic clause structure for each document that has completed text normalization.

The output of this feature is consumed by downstream features such as Clause Categorization, Document Classification, and Document Summary.

---

# Business Requirements

## BRQ-001

The platform shall transform normalized document text into clause-level structure.

## BRQ-002

The detected clause structure shall preserve document reading order.

## BRQ-003

Clause detection shall be tenant-aware and execute only within authenticated tenant context.

## BRQ-004

The feature shall produce traceable and versioned business facts for successful and failed detection outcomes.

## BRQ-005

Clause detection shall be deterministic for the same normalized input and configuration.

---

# Scope

This feature is responsible for:

- Detecting clause boundaries from normalized text.
- Producing ordered clause records.
- Preserving numbering labels when present.
- Persisting clause detection results.
- Emitting detection outcome events.

This feature does not:

- Categorize clauses.
- Classify full documents.
- Summarize document content.
- Generate embeddings.
- Modify legal meaning.
- Perform OCR.

---

# Functional Requirements

## FR-001 Input Dependency

The service shall execute clause detection only when NormalizedText is available for a document.

## FR-002 Canonical Input

The service shall use NormalizedText as the source of truth and shall not mutate it.

## FR-003 Clause Segmentation

The service shall identify clause boundaries and return each clause as a separate ordered unit.

## FR-004 Ordering

The service shall preserve the original reading order of clauses.

## FR-005 Numbering Preservation

When clause numbering exists, the detected result shall preserve the original numbering label.

## FR-006 Deterministic Identifier

Each detected clause shall receive a deterministic identifier unique within the document context.

## FR-007 Result Persistence

The service shall persist the clause detection result in a tenant-aware repository model.

## FR-008 Processing Failure

If clause detection cannot complete, the document shall transition to Failed status and processing shall stop for this stage.

## FR-009 Idempotent Re-execution

Repeated detection requests for the same document revision shall not create duplicate clause records.

## FR-010 Event Publication

The service shall publish ClauseDetectionCompleted when detection succeeds and ClauseDetectionFailed when detection fails.

## FR-011 Correlation

Detection operations and events shall include correlation identifiers.

## FR-012 Security Boundary

The service shall reject execution when authenticated tenant context is missing or invalid.

---

# Non-Functional Requirements

## NFR-001 Performance

For a typical normalized document up to 100 pages, clause detection should complete within 5 seconds under nominal load.

## NFR-002 Reliability

For equivalent input and configuration, clause detection outcomes shall be deterministic.

## NFR-003 Security

The feature shall not expose document content in logs, errors, or events.

## NFR-004 Observability

Structured logs shall include:

- DocumentId
- TenantId
- CorrelationId
- RevisionNumber
- ClauseCount
- ProcessingDuration
- Outcome

## NFR-005 Multi-Tenant Isolation

All reads, writes, and emitted events shall be tenant-scoped.

## NFR-006 Compatibility

Event contracts shall be versioned and backward compatible.

---

# Domain Model

Aggregate Root:

- Document

New Concepts:

- ClauseDetectionResult
- Clause (entity within aggregate boundary)

Value Objects:

- ClauseId
- ClauseNumberLabel (optional)
- ClauseText
- ClauseSpan

State Changes:

- Document moves from Normalized to ClausesDetected on success.
- Document moves to Failed on detection failure.

Detailed model and invariants are defined in domain.md.

---

# API Contract

No new external public API endpoint is introduced by this feature.

Application Contract:

- Command: DetectClauses
- Input:
  - DocumentId
  - TenantId (from authenticated caller context)
  - CorrelationId
- Output (success):
  - DocumentId
  - RevisionNumber
  - ClauseCount
  - ProcessingStage = ClausesDetected
- Output (failure):
  - DocumentId
  - FailureReason
  - ProcessingStage = Failed

Authentication and authorization requirements:

- Caller must be authenticated.
- Caller must be authorized for the target tenant.
- TenantId from request body is not trusted for authorization decisions.

Versioning strategy:

- Application contract version starts at v1.
- Breaking changes require a new version.

---

# Data Model

The persistence model shall include, at minimum:

- DocumentId
- TenantId
- RevisionNumber
- ClauseDetectionStatus
- ClauseDetectionTimestamp
- ClauseDetectionFailureReason (nullable)
- Clauses[] where each clause includes:
  - ClauseId
  - Sequence
  - ClauseNumberLabel (nullable)
  - Text
  - SpanStart
  - SpanEnd

Data constraints:

- Sequence is 1-based and strictly increasing.
- SpanStart and SpanEnd are non-negative and SpanStart < SpanEnd.
- ClauseId is unique within a document revision.

---

# Events

## ClauseDetectionCompleted (v1)

Published when clause detection succeeds.

Required fields:

- EventId
- EventVersion
- OccurredAt
- CorrelationId
- CausationId (when available)
- DocumentId
- TenantId
- RevisionNumber
- ClauseCount

## ClauseDetectionFailed (v1)

Published when clause detection fails.

Required fields:

- EventId
- EventVersion
- OccurredAt
- CorrelationId
- CausationId (when available)
- DocumentId
- TenantId
- RevisionNumber
- FailureReason

Event semantics:

- Events represent completed business facts.
- Events are immutable.
- Events are emitted only after state persistence for the corresponding outcome.

---

# Acceptance Criteria

## AC-001 Successful segmentation

Given a normalized document with clearly separated clauses

When clause detection executes

Then clauses are detected in reading order and persisted.

## AC-002 Numbering preservation

Given a normalized document containing numbered clauses

When clause detection executes

Then each detected clause preserves the original numbering label when present.

## AC-003 Tenant isolation

Given a caller authenticated for Tenant A

When the caller requests clause detection for a document owned by Tenant B

Then the request is rejected and no clause records are produced.

## AC-004 Deterministic result

Given the same document revision and same configuration

When clause detection executes multiple times

Then the resulting clause set is equivalent and no duplicate clauses are created.

## AC-005 Failure handling

Given a document with invalid normalization state or processing error

When clause detection executes

Then the document transitions to Failed and ClauseDetectionFailed is published.

## AC-006 Observability and privacy

Given any clause detection execution

When logs and events are emitted

Then correlation and outcome fields are present and raw clause text is not emitted in logs or events.

---

# Test Specification

The implementation shall include:

- Unit Tests:
  - Clause segmentation rules
  - Deterministic ClauseId generation
  - Numbering preservation
  - Failure transitions
- Integration Tests:
  - End-to-end workflow from NormalizedText to persisted clauses
  - Tenant isolation enforcement
  - Idempotent re-execution behavior
- Contract Tests:
  - ClauseDetectionCompleted and ClauseDetectionFailed schema/version validation
- Acceptance Tests:
  - AC-001 through AC-006 business scenarios

---

# Out of Scope

- Clause categorization taxonomy
- Legal risk scoring
- Cross-document clause comparison
- Summarization
- Embedding generation
- Human-in-the-loop review UI

---

# Dependencies

Required upstream features:

- Document Ingestion
- Document Type Detection
- Text Extraction
- Text Normalization

Provides functionality for:

- Clause Categorization
- Document Classification
- Document Summary
- Document Embeddings

---

# Open Questions

1. Should clause detection support nested clause hierarchies in v1, or only flat ordered clauses?
2. Should clause text persistence store full text per clause, or offsets only with text resolved from NormalizedText?
3. What is the approved maximum document size and timeout policy for clause detection?
4. Should partial success be allowed when some clauses fail to parse, or must the operation be fully atomic?
5. Are multilingual clause boundary models in scope for v1 or deferred?
