# Global Constitution

**Version:** 1.0.0
**Status:** Active
**Applies To:** All repositories, services, specifications, prompts, agents, skills, workflows, and contributors.

---

# Purpose

This Constitution defines the non-negotiable principles governing the design, implementation, testing, documentation, and evolution of the Convenciones platform.

All human contributors, AI assistants, GitHub Copilot, automation tools, and future services MUST comply with this Constitution.

If any specification, prompt, instruction, or implementation conflicts with this Constitution, this Constitution SHALL prevail.

---

# Guiding Principles

The platform is built around the following engineering philosophies:

* Specification-Driven Development (SDD)
* Domain-Driven Design (DDD)
* Clean Architecture
* Vertical Slice Architecture
* CQRS
* Event-Driven Architecture
* API-First Design
* Secure-by-Design
* AI-Assisted Development

---

# Article I — Specification First

Implementation MUST never precede specification.

Before any feature, service, or change is implemented, the corresponding specifications MUST exist and be approved.

The minimum required specifications are:

* Vision
* Business Requirements
* Functional Requirements
* Non-Functional Requirements
* Domain Model
* API Contract
* Data Model
* Events
* Acceptance Criteria
* Test Specification

No production code SHALL be written before these specifications are available.

---

# Article II — No Assumptions

Requirements MUST NOT be invented.

Whenever information is missing:

* Stop implementation.
* Ask clarifying questions.
* Update the affected specifications.
* Obtain approval before continuing.

AI assistants MUST never fabricate business rules.

---

# Article III — Domain First

The Domain Model is the source of truth.

Business rules MUST belong to the domain layer.

The following are prohibited:

* Anemic Domain Models
* Business logic inside controllers
* Business logic inside repositories
* Business logic inside infrastructure components

Aggregates SHALL enforce business invariants.

Value Objects SHALL be immutable.

Entities SHALL protect their consistency.

---

# Article IV — Architectural Integrity

Every implementation MUST preserve the selected architecture.

The following architectural principles are mandatory:

* Clean Architecture
* Dependency Rule
* Separation of Concerns
* Single Responsibility
* Bounded Context isolation

No implementation shortcut may violate these principles.

---

# Article V — API First

Every public API MUST be designed before implementation.

Every API specification SHALL include:

* OpenAPI 3.x
* Request schemas
* Response schemas
* Error schemas
* Authentication requirements
* Authorization requirements
* Versioning strategy

Implementation MUST conform to the approved API specification.

---

# Article VI — Security By Design

Security is mandatory.

Every service MUST implement:

* HTTPS
* Authentication
* Authorization
* Input validation
* Output validation
* Secure secret storage
* Audit logging
* Least privilege
* Secure defaults

Sensitive information MUST never be stored in source code.

Passwords MUST never be stored in plain text.

---

# Article VII — Multi-Tenant Architecture

Tenant isolation is mandatory.

Every business service SHALL enforce:

* Tenant ownership validation
* Tenant-aware queries
* Tenant-aware commands
* Tenant-aware events
* Tenant-aware authorization

Cross-tenant data access is prohibited unless explicitly specified.

---

# Article VIII — Service Ownership

Each microservice owns:

* its domain
* its database
* its API
* its events
* its business rules

Direct database access between services is forbidden.

Communication between services SHOULD occur through APIs or events.

---

# Article IX — Event-Driven Design

Services SHOULD communicate using events whenever appropriate.

Events MUST:

* be immutable
* be versioned
* include correlation identifiers
* include causation identifiers when applicable

Events SHALL represent completed business facts.

---

# Article X — Observability

Every service MUST provide:

* Structured logging
* Metrics
* Distributed tracing
* Health checks
* Correlation identifiers

Production systems MUST be observable without code modifications.

---

# Article XI — Testability

Every feature MUST include:

* Unit Tests
* Integration Tests
* Contract Tests
* Acceptance Tests

Tests are part of the deliverable.

Code without tests SHALL be considered incomplete.

---

# Article XII — AI-Assisted Development

Artificial Intelligence is a development accelerator, not the source of truth.

All AI-generated code MUST:

* Follow approved specifications.
* Follow this Constitution.
* Pass all quality gates.
* Be reviewed before merging.
* Never introduce undocumented behavior.

AI MUST NOT make architectural decisions without specification updates.

---

# Article XIII — Documentation

Documentation is part of the product.

Every architecture, domain, workflow, API, or business rule modification MUST update its corresponding specification.

Outdated documentation SHALL be treated as a defect.

---

# Article XIV — Backward Compatibility

Public APIs and integration events are contracts.

Breaking changes require:

* explicit approval
* versioning
* migration strategy
* updated documentation

Compatibility SHALL be preserved whenever possible.

---

# Article XV — Quality Gates

No implementation may be merged unless:

* Build succeeds
* Static analysis passes
* Security analysis passes
* Tests succeed
* Documentation is updated
* Specifications remain synchronized

---

# Article XVI — Definition of Done

A feature SHALL be considered complete only when:

* Specifications are approved.
* Implementation is complete.
* Tests pass.
* Documentation is updated.
* Security review is completed.
* Code review is approved.
* Quality gates succeed.

---

# Article XVII — Architectural Decision Records

Significant architectural decisions that introduce, modify, or deprecate architectural patterns, technologies, cross-cutting concerns, integration strategies, or service boundaries MUST be documented using an Architectural Decision Record (ADR) before implementation.

Every implementation SHALL follow the latest approved ADR.

---

# Article XVIII — Governance

This Constitution is the highest-level engineering document of the project.

All specifications, prompts, AI instructions, workflows, and implementations SHALL comply with it.

Changes to this Constitution require architectural review and explicit approval.

---

# Final Rule

**When in doubt, update the specification—not the code.**
