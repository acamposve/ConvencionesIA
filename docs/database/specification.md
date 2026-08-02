# Database Persistence Specification

## Status

Draft for review. No implementation changes are planned at this stage.

## Objective

Define the expected database persistence behavior for the document ingestion domain based on the current architecture, domain model, and implementation notes.

## Business Context

The platform must persist document ingestion workflow artifacts in a secure, multi-tenant, auditable, and testable way. The persistence layer must support the current document lifecycle, clause analysis, classification, summarization, embedding generation, and processing history without violating the existing Clean Architecture boundaries.

## Business Requirements

1. Persist document records for each accepted ingestion request.
2. Enforce tenant isolation so data is only accessible within the authorized tenant context.
3. Record lifecycle progression from pending acceptance through processing stages and final outcomes.
4. Preserve revision history for document state changes.
5. Persist clause extraction results and clause category assignments.
6. Persist document-level classification, summary, and embedding outputs.
7. Maintain an audit trail of processing actions and state changes.
8. Preserve idempotency semantics for duplicate submissions.
9. Support future search and analytics scenarios without forcing a redesign.

## Functional Requirements

### Tenant and security

- The database design must support a tenant-scoped model.
- Every tenant-owned entity must be associated with a tenant identifier.
- Application code must enforce tenant filtering in repository implementations.
- The design must allow the use of row-level security or application-managed tenant scoping.

### Document persistence

- A document must be stored as the aggregate root for the ingestion process.
- The document record must store current state, outcome, processing stage, metadata, and text values.
- The document must support rehydration from persisted state.

### Revision history

- Every meaningful state change must create or update a revision record.
- Revisions must retain processing stage and outcome information.

### Clause and analysis persistence

- Extracted clauses must be stored and linked to the owning document.
- Clause category assignments must be stored with confidence values.
- Classification, summary, and embedding outputs must be stored independently from the primary document record to preserve traceability.

### Auditability

- Processing events must be logged for key workflow transitions.
- Audit rows must be retained even when a document is rejected or fails.

## Non-Functional Requirements

### Security

- Tenant boundaries must not be bypassed.
- Sensitive values must be handled through secure configuration and not hard-coded.
- Authorization context must be validated before persistence operations.

### Reliability

- Writes must be atomic where possible.
- The schema must support idempotent processing and duplicate suppression.
- Data must remain recoverable through migrations and backups.

### Performance

- Indexes should support tenant-scoped queries, revision lookups, and clause searches.
- Large text and vector data should be stored and indexed in a way that avoids unnecessary full-table scans.

### Maintainability

- The schema must be understandable and aligned to the existing domain model.
- The design should allow incremental rollout and future extension.

## Data Model Requirements

The persistence design must cover the following entities:

- Tenant
- Document
- DocumentRevision
- Clause
- ClauseCategoryAssignment
- DocumentClassification
- DocumentSummary
- DocumentEmbedding
- ProcessingEvent

### Core relationships

- One tenant has many documents.
- One document has many revisions.
- One document has many clauses.
- One clause has many category assignments.
- One document has many classifications, summaries, and embeddings.
- One tenant has many processing events.

## Event and Integration Requirements

The persistence layer should be compatible with the existing event-driven workflow and should not prevent the publication of domain events.

The following behaviors are expected:

- persistence happens after successful domain validation
- failures are captured in the workflow and stored as part of the document outcome
- processing events reflect real workflow transitions for observability

## Acceptance Criteria

1. A document can be stored and rehydrated for a specific tenant.
2. Document revisions are created for state changes.
3. Clauses and their category assignments can be persisted independently.
4. Classification, summary, and embedding outputs can be stored and retrieved.
5. Tenant-scoped queries return only data belonging to the current tenant.
6. Duplicate ingestion requests do not create conflicting persisted state.
7. Processing events and audit records are written for major workflow transitions.
8. The database design can be implemented incrementally without changing the domain model.

## Test Specification

### Unit tests

- Validate repository-level tenant scoping behavior.
- Validate mapping logic between domain entities and persistence records.

### Integration tests

- Verify persistence of a full document workflow end to end.
- Verify revision history and event logging.

### Contract tests

- Verify repository contracts remain compatible across implementations.

### Acceptance tests

- Verify supported ingestion scenarios persist correctly.
- Verify rejected or failed workflows still preserve traceability.

## Out of Scope for the Initial Phase

- Full-text search optimization
- Advanced analytics dashboards
- Blob storage for original files
- Vector search production tuning
- Cross-tenant administrative workflows
