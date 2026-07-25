# Document Ingestion Specification

**Feature:** Document Ingestion  
**Version:** 1.0.0  
**Status:** Draft

---

## Purpose

Define the business capability to receive a document into the platform, validate it within tenant boundaries, register its provenance, and produce an accepted ingestion result that enables downstream processing.

This specification is feature-level and does not define implementation details.

---

## Scope

### In Scope

- Receive a document from supported sources.
- Validate tenant context and ownership.
- Validate accepted file format and basic metadata.
- Register ingestion outcome (accepted or rejected).
- Persist ingestion-relevant business state in the Document aggregate.
- Emit a business fact when ingestion is completed.

### Out of Scope

- OCR execution.
- Text extraction and normalization.
- Clause detection and categorization.
- AI enrichment.
- Storage provider implementation details.

---

## Business Inputs

1. Tenant context.
2. Document source.
3. File payload or source reference.
4. Correlation context.

---

## Business Outputs

1. Accepted ingestion with document identity and initial processing stage.
2. Rejected ingestion with business reason.
3. Domain event indicating ingestion completion when accepted.

---

## Functional Requirements

### FR-1 Tenant Isolation

Every ingestion operation must execute in a valid tenant context.

### FR-2 Supported Sources

The platform must accept ingestion only from approved business sources:

- Upload
- URL
- Cloud Storage
- External Integration

### FR-3 Supported Formats

The platform must accept only supported document types:

- PDF
- Word documents
- Images

### FR-4 Metadata Registration

On accepted ingestion, the platform must register business-relevant metadata at minimum:

- File size
- MIME type
- Language (when available)
- Page count (when available)
- Author (when available)
- Creation date (when available)

### FR-5 Source Traceability

Every ingested document must preserve source/provenance information for auditability.

### FR-6 Ingestion Outcome

An ingestion attempt must end in one of two business outcomes:

- Accepted
- Rejected

### FR-7 Rejection Reasons

Rejected ingestion must provide explicit business reason categories, at minimum:

- Invalid tenant context
- Unsupported source
- Unsupported format
- Missing required payload/reference
- Validation failure

### FR-8 Initial Lifecycle State

An accepted document must enter the initial processing stage defined for pipeline progression.

### FR-9 Domain Event Emission

When ingestion is accepted, a completed business fact must be emitted as a versioned domain event including:

- Document identity
- Tenant identity
- Correlation identifier
- Timestamp

### FR-10 Idempotency Expectation

The ingestion flow must prevent unintended duplicate accepted ingestions according to an approved idempotency policy.

---

## Non-Functional Requirements

### NFR-1 Security

- Enforce authentication.
- Enforce authorization.
- Validate all inputs.
- Avoid exposing internal error details.

### NFR-2 Multi-Tenant Guarantees

- No cross-tenant access.
- Tenant ownership is explicit in accepted ingestion state.

### NFR-3 Observability

- Include correlation identifier in logs/traces.
- Emit structured audit-relevant ingestion records.

### NFR-4 Reliability

- The operation must produce deterministic accepted/rejected outcomes for equivalent inputs.

### NFR-5 Contract Compatibility

- Event and API contracts must remain backward compatible or be versioned when changed.

---

## Business Rules

1. A document belongs to exactly one tenant.
2. Tenant ownership cannot change after acceptance.
3. Only approved sources and formats are eligible for acceptance.
4. Rejected ingestion does not create an accepted processing lifecycle state.
5. Accepted ingestion creates traceable, versioned business state.

---

## Acceptance Criteria

1. Given a valid tenant and supported input, ingestion is accepted and the document enters the initial stage.
2. Given an unsupported source or format, ingestion is rejected with explicit reason.
3. Given missing tenant context, ingestion is rejected.
4. Given accepted ingestion, an ingestion-completed event is emitted with required identifiers.
5. Given any ingestion attempt, traceability data is available for audit and diagnostics.

---

## Open Questions

1. What is the canonical idempotency key policy for duplicate submission detection?
2. Is source URL validation policy business-defined or delegated to source adapters?
3. Which metadata fields are mandatory vs optional at acceptance time?
4. What is the exact initial processing stage name in the shared lifecycle taxonomy?
5. What is the approved event name and versioning baseline for ingestion completion?

## Clarification Decisions

The following implementation decisions are now recorded as the baseline for this feature:

1. Idempotency policy: duplicate submissions are detected by a deterministic idempotency key derived from tenant identity, source type, and a normalized source reference or content fingerprint. Repeated accepted submissions for the same logical document return the existing accepted document identity and do not create a second accepted ingestion.
2. Source URL validation: business policy requires URL-based sources to be validated as supported and well-formed; detailed URL parsing and adapter-specific validation are delegated to the source adapter layer.
3. Metadata at acceptance time: file size, MIME type, and source provenance are mandatory for acceptance. Language, page count, author, and creation date are optional and recorded when available.
4. Initial processing stage: accepted documents enter the shared lifecycle stage "PendingProcessing".
5. Ingestion completion event: the domain event name is "DocumentIngestionCompleted" and the event version baseline is "v1".

---

## Dependencies

- Constitution
- Architecture
- Glossary
- Technology
- Document aggregate domain model

---

## Application Workflow (Implementation Guidance)

The application layer should implement the ingestion use case as a single workflow with the following business steps:

1. Receive the ingestion request with tenant context, source, payload or reference, and correlation context.
2. Validate that the tenant context is present and authorized for the operation.
3. Validate that the source is one of the approved supported sources.
4. Validate that the document format is supported.
5. Validate that the required payload or source reference is present.
6. Derive the idempotency key from tenant identity, source type, and a normalized source reference or content fingerprint.
7. Check whether a matching accepted document already exists for the same idempotency key.
8. If a matching accepted document exists, return the existing accepted document identity and do not create a duplicate accepted ingestion.
9. If no matching accepted document exists, evaluate the remaining metadata and provenance requirements.
10. If all business preconditions are satisfied, accept the document, assign the initial processing stage of PendingProcessing, persist the accepted business state, and emit the DocumentIngestionCompleted domain event.
11. If any precondition fails, reject the ingestion, persist the rejection reason, and return the rejected outcome without creating an accepted processing lifecycle state.

This workflow is intentionally business-oriented and should remain independent from storage-provider and OCR-specific implementation details.

---

## Persistence and State Model (Implementation Guidance)

The persistence layer should preserve the following business state for ingestion:

1. A Document record representing the aggregate root state for the ingestion lifecycle.
2. A TenantId value that is stored with the document and remains immutable after acceptance.
3. A Source and Provenance record that preserves the origin and traceability context of the ingestion attempt.
4. A Metadata record containing at minimum file size, MIME type, and the optional language, page count, author, and creation date values when available.
5. An IngestionOutcome field that records whether the attempt was Accepted or Rejected.
6. A RejectionReason field for rejected attempts, when applicable.
7. A ProcessingStage field that is set to PendingProcessing only for accepted ingestions.
8. An IdempotencyKey field used to detect duplicate submissions and ensure the same logical document is not accepted twice.
9. A DocumentRevision history entry for each accepted transition so the business state remains versioned and auditable.

The persistence model should ensure that rejected ingestions do not create an accepted lifecycle state, while accepted ingestions create a traceable, versioned baseline for downstream processing.

---

## Event and Integration Contract (Implementation Guidance)

When ingestion is accepted, the platform should emit a versioned domain event with the following business contract:

- Event name: DocumentIngestionCompleted
- Event version: v1
- Required attributes:
  - DocumentId
  - TenantId
  - CorrelationId
  - Timestamp
  - Version
- Business semantics:
  - The event represents a completed business fact that the document has been accepted into the ingestion lifecycle.
  - The event must be emitted only after the accepted document state has been persisted.
  - The event should be emitted once per accepted ingestion transition and not for rejected attempts.

For backward compatibility, any future changes to the event contract must remain backward compatible or be introduced as a new versioned event contract.

---

## API Contract (Implementation Guidance)

The ingestion API should expose a request contract that includes:

- Tenant context
- Source type
- Payload or source reference
- Correlation context
- Optional metadata values when supplied

The response contract should include:

- Outcome: Accepted or Rejected
- DocumentId for accepted ingestions
- ProcessingStage for accepted ingestions
- RejectionReason for rejected ingestions
- Provenance and traceability information
- CorrelationId for request correlation

Error responses should remain business-safe and should not expose internal implementation details.

---

## Security, Multi-Tenant, and Observability (Implementation Guidance)

The implementation must enforce:

- Authentication and authorization for all ingestion requests.
- Tenant isolation so only the owning tenant can ingest or access its documents.
- Correlation identifiers in logs, traces, and audit records.
- Structured ingestion audit records for accepted and rejected attempts.
- No cross-tenant access and no disclosure of internal error details.

---

## Testing Scope (Implementation Guidance)

The feature should include:

- Unit tests for domain invariants, eligibility rules, and rejection reasons.
- Integration tests for tenant enforcement, persistence, and event emission.
- Contract tests for API and event compatibility.
- Acceptance tests covering valid ingestion, unsupported source/format rejection, missing tenant rejection, and traceability requirements.

---

## Documentation and Rollout Readiness

The feature should be considered ready when:

- The ingestion lifecycle, supported sources/formats, and business outcomes are documented.
- The open clarifications have been recorded as final decisions.
- The implementation has been reviewed against the architecture, security, and multi-tenant requirements.

---

## Guiding Principle

Ingestion is complete only when the platform can assert tenant-safe provenance, validated eligibility, and a traceable business outcome ready for downstream document intelligence processing.
