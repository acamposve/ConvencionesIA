Status: completed and verified.

Validation evidence: dotnet test .\Convenciones\Convenciones.slnx completed successfully with 104 passed tests, 0 failed, 0 errors.

Perform a complete implementation review remediation for the Document Ingestion service.

Follow the project Constitution and all SDD principles.
Do NOT introduce breaking changes unless absolutely necessary.
Maintain backwards compatibility whenever possible.
Execute the work sequentially. Finish each phase completely before moving to the next one.

After each phase:

- update affected unit tests
- create new tests when necessary
- ensure all tests pass
- avoid suppressing warnings
- keep Clean Architecture and DDD boundaries intact

====================================================
PHASE 1 - FIX AGGREGATE REHYDRATION (CRITICAL)
====================================================

Problem:

The repository persists Outcome, ProcessingStage and State but reconstructs Document by only distinguishing between Accepted and Rejected.

Failed documents can become Accepted after reload.

Files:

- FileSystemDocumentRepository.cs

Goals:

1. Rehydrate the aggregate using the complete persisted state.
2. Restore:
   - DocumentState
   - ProcessingOutcome
   - ProcessingStage
3. Correctly restore Failed state.
4. Ensure the aggregate after loading is semantically identical to the aggregate before persistence.
5. Do not bypass domain invariants.
6. If necessary, introduce a dedicated rehydration factory inside the aggregate instead of abusing public behavior methods.
7. Preserve event consistency.

Tests:

Create/update tests proving round-trip persistence for:

- Pending
- Accepted
- Rejected
- Failed

The aggregate before Save and after Load must be equivalent.

====================================================
PHASE 2 - PERSIST ALL DOMAIN DATA
====================================================

Problem:

DetectedDocumentType and Revision History are not persisted although required by the persistence contract.

Files:

- FileSystemDocumentRepository.cs
- DocumentPersistenceContract.cs

Goals:

1. Persist every property defined by DocumentPersistenceContract.
2. Include:
   - DetectedDocumentType
   - Revision history
3. Restore them during rehydration.
4. Remove any mismatch between repository implementation and persistence contract.

Tests:

Add round-trip persistence tests verifying every persisted field.

====================================================
PHASE 3 - MULTI-TENANT SECURITY
====================================================

Problem:

If callerTenantId is null the endpoint trusts request.TenantId.

Files:

- DocumentIngestionEndpoint.cs
- TenantSecurityGuard.cs

Goals:

1. Never trust tenant information coming from the request body.
2. Authorization must only use authenticated caller context.
3. Decide a single strategy:
   - reject missing callerTenantId
   OR
   - obtain tenant from authenticated identity
4. Remove every authorization path that depends on request.TenantId.
5. Ensure tenant spoofing is impossible.

Tests:

Add tests covering:

- valid tenant
- missing caller tenant
- mismatched tenant
- spoofing attempts

====================================================
PHASE 4 - IDEMPOTENCY + CONCURRENCY
====================================================

Problem:

Current flow performs:

check
then
save

without synchronization.

The in-memory repository also uses a non-thread-safe Dictionary.

Files:

- IngestDocumentUseCase.cs
- InMemoryDocumentRepository.cs

Goals:

1. Make idempotency atomic.
2. Remove race conditions.
3. Make the in-memory repository thread-safe.
4. If necessary:
   - use ConcurrentDictionary
   - introduce repository-level atomic operations
   - avoid global locks if a finer-grained solution exists.
5. Preserve current repository abstractions.

Tests:

Create concurrent ingestion tests proving that duplicate requests with identical idempotency key only create one document.

====================================================
PHASE 5 - REMOVE DOMAIN DUPLICATION
====================================================

Problem:

Validation rules are duplicated between:

- Document entity
- DocumentIngestionService

Also DocumentPersistenceContract is not integrated into the real persistence flow.

Goals:

1. Make the aggregate the single source of truth for business invariants.
2. Remove duplicated validation logic.
3. Integrate DocumentPersistenceContract into the persistence implementation.
4. Ensure repository serialization is driven by the contract instead of duplicated mappings.

Tests:

Update all affected tests.

====================================================
PHASE 6 - PERFORMANCE
====================================================

Problem:

GetByTenantAndIdempotencyKey scans every JSON document.

Goals:

Improve lookup performance.

Preferred approaches:

- secondary index
- metadata index
- lightweight lookup table

Do not sacrifice correctness.

Maintain compatibility with existing persisted files if feasible.

====================================================
PHASE 7 - TEST DEPENDENCIES
====================================================

Problem:

xunit.runner.visualstudio version mismatch.

Goals:

Update all test projects to compatible package versions.

Remove warnings.

====================================================
FINAL VALIDATION
====================================================

When all phases are complete:

1. Search for any remaining TODO/FIXME related to persistence, tenant security or idempotency.
2. Remove dead code.
3. Remove obsolete methods.
4. Ensure no duplicated logic remains.
5. Verify all projects compile.
6. Verify all unit tests pass.
7. Verify no new analyzer warnings were introduced.
8. Produce a final summary including:
   - files modified
   - architectural decisions
   - remaining technical debt (if any)
   - justification for every non-trivial design decision.