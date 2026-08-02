# Relational Database Model

## Purpose

This document translates the domain concepts into a relational database model and highlights the relationships between the main entities.

## Entity Relationship Overview

```text
tenants
  1:N documents
  1:N processing_events

documents
  1:N document_revisions
  1:N clauses
  1:N document_classifications
  1:N document_summaries
  1:N document_embeddings

document_revisions
  1:N clauses

clauses
  1:N clause_category_assignments
```

## Entity Notes

### Tenant
- Represents the business tenant.
- All tenant-owned data should be scoped to this entity.

### Document
- Represents the root aggregate.
- Includes current state, processing stage, outcomes, metadata, and raw/normalized text.

### DocumentRevision
- Stores an immutable historical snapshot of the document lifecycle.
- Useful for auditing and debugging processing transitions.

### Clause
- Stores extracted clause text and positions.
- Should remain linked to the document and optionally to a specific revision.

### ClauseCategoryAssignment
- Stores one or more category assignments for a clause.
- Confidence is part of the assignment, not the clause itself.

### DocumentClassification
- Stores one or more classification outcomes for the document.
- The current implementation uses a single classification result, but this table allows history.

### DocumentSummary
- Stores summary outputs generated for the document.
- Can be versioned later if multiple summaries are produced.

### DocumentEmbedding
- Stores vector data and status for generated embeddings.
- The vector type should be implementation-specific; PostgreSQL with pgvector is a strong fit.

## Recommended Column Types

- Use UUID for identifiers to simplify distributed and future integration scenarios.
- Use TIMESTAMPTZ for audit timestamps.
- Use TEXT for long-form content such as summary text or raw text.
- Use DECIMAL for confidence scores with fixed precision.
- Use JSONB if future metadata becomes more dynamic than the initial schema.

## Data Integrity Considerations

- Prevent orphaned rows with foreign keys.
- Enforce uniqueness on tenant-document identity and revision versioning.
- Keep the domain validation rules reflected in constraints where possible.
- Consider soft delete or archival strategy for long-term retention.

## Suggested Evolution Path

1. Start with the core tables for documents and revisions.
2. Add clauses and classification artifacts.
3. Add embeddings and search-oriented indexes.
4. Introduce observability and audit trails.
5. Add storage for original files through blob or object storage.
