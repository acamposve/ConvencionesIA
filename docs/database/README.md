# Database Design Proposal

## Objective

This document proposes an initial relational database model for the document ingestion domain described in the solution. The goal is to support multi-tenant document processing, lifecycle tracking, clause-level analysis, classification, summarization, embeddings, and auditability without introducing implementation changes in the application code yet.

## Recommended Database Platform

A relational database such as PostgreSQL is the best fit for this solution.

### Why PostgreSQL

- Strong support for JSONB, which is useful for flexible metadata and future enrichment.
- Mature transactional model and concurrency control.
- Excellent support for indexing, full-text search, and extensions such as pgvector.
- Good fit for multi-tenant workloads when combined with row-level security or application-level tenant filtering.
- Widely adopted in enterprise environments and compatible with .NET tooling.

### Alternative

- SQL Server is also a valid option if the organization already standardizes on Microsoft technologies and wants tighter integration with the .NET ecosystem.
- PostgreSQL is preferred here because it provides a more flexible and future-proof foundation for AI-oriented features such as embeddings and search.

## Design Principles

- Preserve the existing domain model semantics.
- Enforce tenant isolation at the data layer.
- Treat documents as aggregates with revisions and subordinate entities.
- Keep processing state and outcomes auditable.
- Support future extension for persistence, search, and analytics.

## Core Entities

The proposed schema is organized around the following logical entities:

- Documents
- Document revisions
- Clauses
- Clause category assignments
- Document classifications
- Document summaries
- Document embeddings
- Tenants
- Processing events or audit logs

## Proposed Relational Model

### 1. Tenants

A tenant owns all document-related data and is the primary security boundary.

```sql
CREATE TABLE tenants (
    tenant_id UUID PRIMARY KEY,
    tenant_code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 2. Documents

The document table represents the aggregate root and stores the current state of the document as well as its core metadata.

```sql
CREATE TABLE documents (
    document_id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(tenant_id),
    document_key VARCHAR(255) NOT NULL,
    source_reference TEXT NOT NULL,
    source_name VARCHAR(255),
    document_type VARCHAR(50) NOT NULL,
    format VARCHAR(50) NOT NULL,
    ingestion_state VARCHAR(50) NOT NULL,
    current_outcome VARCHAR(50) NOT NULL,
    current_processing_stage VARCHAR(50) NOT NULL,
    correlation_id VARCHAR(255),
    idempotency_key VARCHAR(255),
    file_size_bytes BIGINT NOT NULL,
    mime_type VARCHAR(255) NOT NULL,
    language VARCHAR(50),
    page_count INT,
    author VARCHAR(255),
    creation_date TIMESTAMPTZ,
    raw_text TEXT,
    normalized_text TEXT,
    rejection_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, document_key)
);
```

### 3. Document Revisions

Each change in processing stage or outcome should be stored as a revision for traceability.

```sql
CREATE TABLE document_revisions (
    revision_id UUID PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    version INT NOT NULL,
    revision_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    outcome VARCHAR(50) NOT NULL,
    processing_stage VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (document_id, version)
);
```

### 4. Clauses

Clauses are extracted from the document content and should be traceable to a specific document revision.

```sql
CREATE TABLE clauses (
    clause_id UUID PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    revision_id UUID REFERENCES document_revisions(revision_id),
    clause_key VARCHAR(255) NOT NULL,
    text TEXT NOT NULL,
    start_index INT NOT NULL,
    end_index INT NOT NULL,
    label VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (document_id, clause_key)
);
```

### 5. Clause Category Assignments

Classification results for individual clauses should be stored separately from the clause itself because confidence and category may evolve.

```sql
CREATE TABLE clause_category_assignments (
    clause_id UUID NOT NULL REFERENCES clauses(clause_id) ON DELETE CASCADE,
    category_code VARCHAR(100) NOT NULL,
    confidence_score DECIMAL(5,4) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (clause_id, category_code, created_at)
);
```

### 6. Document Classifications

The document-level classification result can be stored as one row per document.

```sql
CREATE TABLE document_classifications (
    document_id UUID NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    classification_code VARCHAR(100) NOT NULL,
    confidence_score DECIMAL(5,4) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (document_id, created_at)
);
```

### 7. Document Summaries

The generated summary should be stored alongside the document and optionally versioned.

```sql
CREATE TABLE document_summaries (
    summary_id UUID PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    summary_text TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 8. Document Embeddings

Embeddings are often large and may be stored in a separate table for maintainability.

```sql
CREATE TABLE document_embeddings (
    embedding_id UUID PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    embedding_vector VECTOR(1536),
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 9. Processing Audit Log

A simple audit log helps preserve observability and support investigations.

```sql
CREATE TABLE processing_events (
    event_id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(tenant_id),
    document_id UUID REFERENCES documents(document_id),
    event_type VARCHAR(100) NOT NULL,
    event_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Indexing Recommendations

Suggested indexes for operational performance:

```sql
CREATE INDEX idx_documents_tenant_created ON documents(tenant_id, created_at DESC);
CREATE INDEX idx_documents_state ON documents(ingestion_state, current_processing_stage);
CREATE INDEX idx_documents_correlation ON documents(correlation_id);
CREATE INDEX idx_clauses_document ON clauses(document_id);
CREATE INDEX idx_clauses_label ON clauses(label);
CREATE INDEX idx_clause_assignments_category ON clause_category_assignments(category_code);
CREATE INDEX idx_processing_events_document ON processing_events(document_id, created_at DESC);
```

## Multi-Tenant Strategy

Because the domain explicitly includes tenant boundaries, the database design should enforce tenant isolation in two layers:

1. Application layer
   - Every query must include tenant filtering.
   - Repository implementations should scope all reads and writes to the current tenant.

2. Database layer
   - PostgreSQL row-level security or a tenant_id column in every tenant-owned table is recommended.
   - If row-level security is used, it should be implemented carefully and tested rigorously.

## Data Lifecycle Notes

The model should support the following lifecycle transitions:

- Pending acceptance
- Accepted
- Rejected
- Failed
- Processing stage progression from pending to embedded

Each transition should create or update a revision and be logged as a processing event.

## Considerations for Future Evolution

The initial schema is intentionally pragmatic. It can evolve to support:

- Full-text search over text and summaries
- Vector similarity search for embeddings
- Separate blob storage for original files
- Partitioning by tenant or time if the dataset grows significantly
- Event sourcing or outbox patterns for reliable integration

## Related documents

- [Rollout and rollback plan](rollout-and-rollback.md)
- [PostgreSQL migration plan](postgresql-migration-plan.md)

## Summary

PostgreSQL is the recommended relational database for this platform. The proposed schema covers documents, revisions, clauses, classifications, summaries, embeddings, and tenant-scoped auditability in a way that aligns with the current domain model and future persistence work.
