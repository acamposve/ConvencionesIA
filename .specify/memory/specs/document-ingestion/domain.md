# Domain Model - Document Ingestion

**Feature:** Document Ingestion  
**Version:** 1.0.0  
**Status:** Draft

---

## Purpose

Define the domain model slice required to support the Document Ingestion feature.

This model focuses on business consistency, tenant isolation, and ingestion lifecycle decisions. It does not include technical implementation concerns.

---

## Clarified Domain Decisions

The domain model will use the following business decisions to support implementation:

- The Document aggregate is the authoritative boundary for ingestion decisions and state transitions.
- An ingestion attempt produces exactly one business outcome: Accepted or Rejected.
- An accepted document must have a single TenantId, a supported source, a supported format, required provenance metadata, and an initial processing stage of PendingProcessing.
- A rejected document must retain a RejectionReason and must not enter any accepted lifecycle stage.
- Duplicate submissions are resolved by an IdempotencyKey that maps to the existing accepted document when the logical document is already known.

---

## Aggregate Boundary

### Aggregate Root: Document

The Document aggregate is the consistency boundary for ingestion.

Why this belongs in the Domain:

- Ingestion acceptance/rejection is a business decision.
- Tenant ownership and source provenance are business constraints.
- Initial lifecycle state must be protected by aggregate invariants.

---

## Entities

### 1. Document (Aggregate Root)

Responsibilities:

- Enforce tenant ownership and preserve tenant immutability after acceptance.
- Enforce source and format eligibility according to the approved business taxonomy.
- Decide ingestion outcome as Accepted or Rejected.
- Assign the initial processing stage of PendingProcessing when accepted.
- Preserve source provenance, metadata, correlation context, and idempotency context.
- Prevent duplicate accepted ingestions by resolving a matching IdempotencyKey to the existing accepted document.

Why in Domain:

- These responsibilities encode business policy and consistency rules.

### 2. DocumentRevision

Represents a versioned historical business snapshot of Document state.

Responsibilities:

- Preserve an immutable historical record of accepted state transitions.
- Support auditability, traceability, and compatibility over time.
- Record the version, timestamp, and outcome associated with each accepted ingestion transition.

Why in Domain:

- Versioning and historical consistency are business governance concerns.

---

## Value Objects

### DocumentId

Unique business identity for a document.

Why in Domain:

- Required for traceability and event identity.

### TenantId

Identity of the owning tenant.

Why in Domain:

- Mandatory to enforce multi-tenant boundaries.

### DocumentSource

Business origin of the document (Upload, URL, Cloud Storage, External Integration).

Why in Domain:

- Eligibility and provenance policies depend on source.

### DocumentMetadata

Descriptive business metadata registered at ingestion (size, MIME type, language, page count, author, creation date).

Why in Domain:

- Metadata impacts acceptance rules and audit interpretation.

### Provenance

Business traceability information describing the document source, source reference, and related audit context.

Why in Domain:

- Provenance is required for source traceability and auditability.

### CorrelationId

Identifier linking the ingestion operation across requests/events.

Why in Domain:

- Required for traceability of completed business facts.

### IdempotencyKey

A deterministic business key used to recognize duplicate submissions for the same logical document.

Why in Domain:

- Prevents unintended duplicate accepted ingestions.

### ProcessingStage

Current business lifecycle stage of the document.

Why in Domain:

- Stage progression controls valid business operations.

### RejectionReason

Categorized business reason for ingestion rejection.

Why in Domain:

- Rejection explanation is part of business behavior, not transport-only error handling.

---

## Invariants

1. A Document must always have exactly one TenantId.
2. TenantId is immutable after accepted ingestion.
3. A Document source must be from approved source taxonomy.
4. Accepted ingestion requires a supported document format.
5. Accepted ingestion requires required provenance and metadata minimums, including file size, MIME type, and traceable source provenance.
6. Ingestion outcome is mutually exclusive: Accepted or Rejected.
7. Rejected ingestion cannot transition to processing stages reserved for accepted lifecycle.
8. Accepted ingestion must assign the initial ProcessingStage of PendingProcessing.
9. A duplicate submission that matches the same IdempotencyKey for the same tenant and logical source cannot create a second accepted ingestion.
10. Domain-changing ingestion transitions must update version through DocumentRevision policy.
11. Any emitted ingestion-completed fact includes DocumentId, TenantId, CorrelationId, version, and timestamp.

Why these belong in Domain:

- They protect business consistency, tenant isolation, and lifecycle semantics.

---

## Responsibilities

### Document Aggregate

- Validate ingestion preconditions.
- Guard state transitions.
- Keep provenance and ownership consistent.
- Produce ingestion-related domain facts when transitions complete.

### DocumentRevision

- Persist immutable business history semantics for versioned transitions.

### Value Objects

- Encapsulate validation-worthy business concepts.
- Prevent primitive, ambiguous state in aggregate decisions.

---

## Relationships

1. Document 1 -> N DocumentRevision
2. Document 1 -> 1 Tenant (by TenantId reference)
3. Document may emit ingestion lifecycle domain events

Why these relationships belong in Domain:

- They represent ownership, traceability, and communication of completed business facts.

---

## Domain vs Non-Domain Separation

Outside this domain model (must remain Application/Infrastructure):

- File binary transport and streaming mechanics
- MIME sniffing library specifics
- Cloud storage SDK behavior
- OCR/AI provider integration
- SQL and repository implementation details
- API endpoint request/response shaping

Rationale:

- Technical mechanisms can change; business invariants must remain stable.

---

## Potential Future Extensions (Not Implemented)

1. Tenant-specific ingestion policies
Description: configurable accepted source/format matrices by tenant.

2. Advanced duplicate detection
Description: semantic duplicate checks beyond deterministic idempotency key.

3. Ingestion risk flags
Description: domain-level fraud or suspicious-origin indicators.

4. Policy-driven rejection taxonomy
Description: richer rejection categories with compliance mapping.

5. Source trust scoring
Description: business trust score attached to source provenance.

6. Redaction-at-ingestion intent
Description: capture legal/privacy handling intent before downstream stages.

Each extension requires specification updates (glossary/domain/events/api) and approval before implementation.

---

## Resolved Clarifications

The following business clarifications have been resolved for the domain model:

1. Supported document formats are represented by the approved source/format taxonomy from the feature specification.
2. The initial processing stage is PendingProcessing.
3. Mandatory acceptance metadata includes file size, MIME type, and source provenance; language, page count, author, and creation date are optional when available.
4. Accepted ingestion transitions create a new versioned DocumentRevision entry.
5. The ingestion-completed domain event is DocumentIngestionCompleted with version v1.

---

## Guiding Principle

The ingestion domain protects tenant-safe admission into the platform: only eligible, traceable, and policy-compliant documents can enter the processing lifecycle.
