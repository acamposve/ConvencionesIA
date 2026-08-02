# Database Implementation Notes

## Recommended Approach

The current solution is already structured around abstractions and domain concepts, so the persistence layer can be introduced incrementally without disrupting the domain model.

## Suggested Phasing

### Phase 1: Foundation
- Introduce a persistence abstraction for repositories.
- Create the tenant-aware database context.
- Implement the initial schema for tenants, documents, and revisions.

### Phase 2: Core Processing Data
- Add persistence for clauses, classifications, summaries, and embeddings.
- Ensure all writes happen through the aggregate root or application services.

### Phase 3: Observability
- Add processing-event logging and audit reporting.
- Expose health and integrity checks.

### Phase 4: Search and Analytics
- Add full-text indexing and vector search capabilities.
- Consider materialized views for reporting.

## Repository Design Guidance

- Keep repository implementations behind the existing contracts.
- Use tenant-aware queries so the application cannot leak data across boundaries.
- Preserve idempotency semantics by using the existing idempotency key or a domain-derived unique constraint.

## Security Guidance

- Never trust tenant context from the client.
- Inject tenant information from authentication or request context.
- Apply authorization checks at the service boundary before any persistence call.

## Operational Guidance

- Enable database migrations from the start.
- Capture query performance and indexing hotspots early.
- Use environment-specific connection strings and secrets management.
- Plan for backup, retention, and disaster recovery from the beginning.
