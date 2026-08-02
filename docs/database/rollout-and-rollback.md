# Rollout and rollback plan

## Objective

Provide a pragmatic rollout path for the current SQLite-backed persistence foundation while keeping the door open for a PostgreSQL-based production deployment later.

## Rollout strategy

1. Deploy the application with the existing SQLite repository implementation for non-production or early-adoption environments.
2. Keep the repository abstraction and tenant-aware query contract unchanged so the storage backend can be swapped without changing use cases.
3. Enable environment-based configuration for the connection string so the deployment can move from local SQLite to a managed database without code changes.
4. Validate persistence and idempotency flows in staging before any production cutover.

## Preconditions

- The application must be configured with a valid connection string.
- The database directory or service account must have write permissions.
- The deployment pipeline should capture the database file location or connection string as a deployment secret or configuration value.
- A backup or export process should exist before enabling write-heavy production traffic.

## Deployment steps

1. Provision the target database environment.
2. Set the connection string in configuration or environment variables.
3. Deploy the application version that uses the configured repository implementation.
4. Verify that the repository initializes the required schema and that new documents are persisted successfully.
5. Confirm tenant-scoped reads and writes behave as expected.

## Rollback steps

1. Stop or drain the deployment that is writing to the new storage backend.
2. Revert to the previous application release or configuration that points to the known-good backend.
3. Restore the previous database backup or copy if data loss or schema divergence is detected.
4. Re-run a smoke test to confirm document ingestion and retrieval are functioning again.

## Recovery expectations

- If the repository is using SQLite, recovery is usually a file-level restore of the database file.
- If a managed relational database is introduced later, use the platform’s backup and point-in-time recovery mechanism.
- Preserve the tenant idempotency key and document identifier values so replays can be safely retried after restoration.

## Observability and operational checks

- Capture repository error logs through the repository operation logger.
- Monitor failed writes, repeated idempotency checks, and tenant-scoped query errors.
- Review the schema initialization path and any migrations applied during deployment.
