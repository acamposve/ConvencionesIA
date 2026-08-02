# ConvencionesIA

ConvencionesIA is a specification-driven platform for building secure, multi-tenant services. The repository currently focuses on a document ingestion capability that demonstrates the architectural principles defined in the global constitution.

## What we are building

We are implementing a document ingestion workflow that:

- accepts and validates document submissions for a specific tenant
- enforces authentication and authorization at the service boundary
- preserves business rules in the domain layer rather than in controllers or infrastructure
- exposes versioned API and event contracts
- suppresses duplicate accepted ingestions through idempotency semantics
- uses automated tests across unit, integration, contract, and acceptance layers

## Architectural principles

This work follows the core principles of the constitution:

- Specification-Driven Development
- Domain-Driven Design
- Clean Architecture
- Vertical Slice Architecture
- CQRS and event-driven patterns
- Secure-by-Design and multi-tenant safeguards
- Observability and testability as first-class requirements

## Current implementation focus

The repository currently contains:

- a domain model for the document aggregate and its value objects
- application workflow logic for accepted and rejected ingestions
- an API contract and endpoint boundary for document submission
- a persistence abstraction with in-memory and file-system implementations for test and runtime scenarios
- contract-driven document rehydration and revision-history preservation
- event publication and audit behavior for accepted ingestions
- MIME-based document-type detection with a distinct DocumentType value object and immediate rejection for unsupported or undetermined types
- a comprehensive test suite covering core scenarios
- document-classification support with domain, application, persistence, event, and test coverage
- document-summary support with domain, application, persistence, event, and test coverage
- document-embedding support with domain, application, persistence, event, and test coverage

## Repository layout

- [src/DocumentIngestion.Domain](src/DocumentIngestion.Domain) - domain models, business rules, and services
- [src/DocumentIngestion.Application](src/DocumentIngestion.Application) - use cases, contracts, repositories, and security enforcement
- [frontend](frontend) - Vite/React frontend for the document ingestion experience
- [tests](tests) - unit, integration, contract, and acceptance tests
- [docs/document-ingestion](docs/document-ingestion) - feature-specific documentation and rollout notes
- [docs/document-details](docs/document-details) - document detail experience documentation

## Quality bar

Every change should:

1. start from approved specifications
2. preserve architecture boundaries
3. include tests
4. keep documentation aligned with implementation
5. respect tenant isolation and security requirements

## Verification

The current feature implementation is covered by automated tests. The documented verification run is:

- command: dotnet test .\Convenciones\Convenciones.slnx
- result: 203 tests passed, 0 failed

## Next steps

- finalize deployment-specific authentication, authorization, logging, and tracing configuration
- validate storage permissions and runtime observability settings for the file-system repository
- keep API and event contracts versioned and backward compatible