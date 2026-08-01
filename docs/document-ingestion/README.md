# Document Ingestion Feature Documentation

## Overview

The Document Ingestion feature now provides a domain-driven ingestion workflow for accepting or rejecting document submissions within tenant boundaries. The implementation covers the domain model, application workflow, endpoint contract, repository persistence abstraction, event publication, security enforcement, and a full test suite. It also includes a text-extraction capability that stores extracted content as RawText, a clause-detection stage that extracts ordered clauses from normalized text, a clause-categorization stage that assigns deterministic category labels and confidence scores to detected clauses, a document-classification stage that assigns a primary business classification with confidence and emits success or failure events, and a document-summary stage that records a primary summary and publishes success or failure business events.

The current implementation uses tenant-aware authorization based on the authenticated caller context, applies repository-level atomic idempotency semantics to suppress duplicate accepted ingestions, and supports contract-driven persistence with both in-memory and file-system repository implementations.

## Approved Business Decisions

- Supported sources: Upload, URL, Cloud Storage, External Integration
- Supported formats: PDF, Word, Image
- Required acceptance metadata: file size, MIME type, and provenance reference
- Optional metadata captured when available: language, page count, author, creation date
- Accepted documents enter the lifecycle stage PendingProcessing
- Accepted ingestions emit the versioned event DocumentIngestionCompleted with version v1
- Rejected ingestions persist a rejection reason and do not create an accepted processing lifecycle state
- Duplicate submissions for the same logical document are suppressed through an idempotency policy keyed to the tenant and the logical submission identity

## Implementation Scope

The current implementation includes:

- Domain aggregate and value objects for document state, provenance, metadata, correlation, and idempotency
- Application workflow orchestration for accepted and rejected outcomes
- MIME-based document-type detection using a distinct DocumentType value object
- Immediate rejection of unsupported or undetermined document types to prevent invalid processing
- API contract for request and response payloads
- Persistence contract for tenant-aware document state and revision history
- Repository abstraction backed by in-memory and file-system implementations, including contract-driven serialization and rehydration
- Event publisher and audit record generation for accepted ingestions and text-extraction outcomes
- A text-extraction workflow that routes PDF, DOCX, and image content through the appropriate strategy and stores extracted text on the document aggregate
- A clause-detection workflow that transforms normalized text into ordered clause entities with optional numbering labels
- A clause-categorization workflow that records category assignments and publishes completion or failure events for the clause pipeline
- A document-classification workflow that records a primary classification and confidence score, persists the classification with the document aggregate, and publishes versioned completion or failure events
- A document-summary workflow that records a primary summary, persists the summary with the document aggregate, and publishes versioned completion or failure events
- Tenant and authentication enforcement at the endpoint boundary, using the authenticated caller tenant as the authoritative tenant context

## Security and Multi-Tenant Considerations

The implementation enforces the following safeguards:

- Authentication is required for ingestion operations
- Authorization uses the authenticated caller tenant context and rejects mismatched or spoofed request-body tenant values
- Tenant context is validated before processing proceeds
- Rejected requests do not expose internal implementation details
- Acceptance and rejection state remain traceable through correlation identifiers and audit records

## Testing and Verification

The feature is covered by:

- Unit tests for domain invariants and rejection rules
- Integration tests for workflow, persistence, security, and event emission
- Contract tests for API and event compatibility
- Acceptance tests for supported and unsupported ingestion scenarios

Verification evidence:

- Command executed: dotnet test .\Convenciones\Convenciones.slnx
- Result: 186 tests passed, 0 failed

## Rollout Readiness Review

### Ready for review

- The implementation is documented against the approved specification decisions
- The workflow and business outcomes are covered by automated tests
- Security and tenant boundaries are enforced at the application boundary
- The API and event contracts are versioned and explicitly tested

### Remaining rollout considerations

- Operational configuration for authentication, authorization, logging, and tracing should be finalized in the target environment
- Any future contract changes should remain backward compatible or use a new versioned event/API contract
- Storage location permissions and runtime observability settings should be validated in the deployment environment

## Recommended Release Gate

The feature is ready for architecture, security, business, and release review. Production rollout should proceed after environment-specific security, storage, and observability settings are confirmed.
