# PostgreSQL migration plan

## Objective

Translate the current SQLite-backed persistence foundation into a PostgreSQL-ready implementation path without changing the domain or application use cases.

## Recommended target architecture

- Keep the existing repository abstraction and use case flow intact.
- Introduce a PostgreSQL repository implementation behind the same interface used by the current SQLite repository.
- Use connection strings and environment-based configuration so the implementation switch is deployment-driven rather than code-driven.

## Migration phases

### Phase 1: Compatibility layer

- Keep the repository contract stable.
- Introduce a PostgreSQL-specific repository class that implements the same operations as the SQLite repository.
- Reuse the same document mapping and tenant-scoping rules.

### Phase 2: Schema alignment

- Translate the SQLite schema definitions into PostgreSQL-native types.
- Replace SQLite-specific syntax with PostgreSQL-compatible DDL.
- Add indexes for tenant-scoped reads and idempotency lookups.

### Phase 3: Operational readiness

- Add connection pooling and health checks.
- Introduce migration tooling for schema creation and upgrades.
- Validate backup, restore, and failover behavior in a non-production environment.

## Data mapping notes

- Use UUID or text-based identifiers consistently.
- Prefer TIMESTAMPTZ for audit fields.
- Keep tenant-scoped queries explicit and filter by tenant identifier in every read path.
- Preserve idempotency semantics through a unique constraint on tenant and idempotency key.

## Rollout notes

- Migrate data in a controlled window rather than as a blind cutover.
- Run both repositories in parallel for comparison in staging if feasible.
- Keep the rollout reversible by preserving the previous database and application configuration.
