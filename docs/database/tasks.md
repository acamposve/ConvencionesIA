# Database Persistence Tasks

## Status

Draft for review. No implementation changes are planned yet.

## Checklist

### 1. Review and confirm scope
- [ ] Confirm the database persistence scope aligns with the current domain model and documentation.
- [ ] Confirm the tenant, document lifecycle, clause, classification, summary, embedding, and audit requirements.
- [ ] Confirm the preferred database platform and migration strategy.

### 2. Define database contract and schema
- [x] Define the initial relational schema for tenants, documents, revisions, clauses, classifications, summaries, embeddings, and processing events.
- [x] Define primary keys, foreign keys, unique constraints, and required indexes.
- [x] Document the expected data types for text, metadata, confidence scores, and vector data.

### 3. Define persistence contracts
- [x] Review the existing persistence abstraction and identify what must be extended for database persistence.
- [x] Define repository responsibilities for create, read, update, and tenant-scoped queries.
- [x] Define how rehydration and revision history will be handled.

### 4. Define repository implementation approach
- [x] Decide on the persistence technology and infrastructure package.
- [x] Define the database context or repository implementation structure.
- [x] Define how environment configuration and connection strings will be managed.

### 5. Define security and multi-tenant behavior
- [x] Define how tenant context will be passed into repository operations.
- [x] Define whether row-level security or application-level tenant filters will be used.
- [x] Define how authorization failures will be surfaced.

### 6. Define migration and rollout plan
- [x] Define the initial migration sequence and backward-compatible rollout approach.
- [x] Define rollback and recovery expectations.
- [x] Define observability requirements for database access and failures.

### 7. Define testing strategy
- [x] Define unit tests for repository mapping and tenant scoping.
- [x] Define integration tests for end-to-end persistence flows.
- [x] Define contract tests for repository compatibility.
- [x] Define acceptance tests for supported and rejected ingestion scenarios.

### 8. Prepare implementation-ready backlog
- [x] Break the work into implementation phases.
- [x] Identify dependencies and sequencing for domain, application, infrastructure, and test work.
- [x] Capture open questions that require business or architecture approval.

## Implementation Phases

### Phase 1: Foundation
- [x] Add the persistence schema for tenants and documents.
- [x] Add document revision support.
- [x] Add migration and configuration scaffolding.

### Phase 2: Processing artifacts
- [x] Add clause and clause assignment persistence.
- [x] Add document classification, summary, and embedding persistence.
- [x] Add processing event logging.

### Phase 3: Security and integration
- [x] Wire tenant-aware repository behavior.
- [x] Validate repository integration with the existing use cases.
- [x] Add observability and error handling.

### Phase 4: Validation and rollout
- [x] Add automated tests.
- [x] Validate migration readiness and operational setup.
- [x] Prepare rollout and rollback documentation.
