# Repository Structure

**Version:** 1.0.0

**Status:** Draft

---

# Purpose

This document defines the standard repository structure used across the platform.

Every microservice SHALL follow this structure unless an approved Architectural Decision Record (ADR) specifies otherwise.

A consistent repository layout improves maintainability, discoverability, onboarding, automation, and AI-assisted development.

---

# Repository Principles

Every repository shall:

* Represent a single bounded context.
* Own its complete implementation.
* Be independently deployable.
* Be independently testable.
* Be independently versioned.

Repositories shall never contain multiple unrelated business domains.

---

# Standard Repository Layout

```text
Repository/

├── .github/
│   ├── workflows/
│   └── copilot/
│
├── .specify/
│   ├── memory/
│   └── specs/
│
├── docs/
│
├── src/
│   ├── Service.Api/
│   ├── Service.Application/
│   ├── Service.Domain/
│   ├── Service.Infrastructure/
│   └── Service.Contracts/
│
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   ├── ContractTests/
│   └── AcceptanceTests/
│
├── docker/
│
├── scripts/
│
├── .editorconfig
├── .gitignore
├── docker-compose.yml
├── Directory.Packages.props
├── Directory.Build.props
└── README.md
```

---

# .specify

Contains every project specification.

```
.specify/

memory/
specs/
```

---

## memory

Contains shared engineering knowledge.

Example:

```
constitution.md
architecture.md
technology.md
repository-structure.md
glossary.md
coding-standards.md
```

These documents apply to every feature.

---

## specs

Contains every business feature.

Each feature owns its own specification.

Example:

```
specs/

document-ingestion/
document-classification/
text-extraction/
clause-detection/
```

---

# Feature Structure

Each feature is represented by its own directory.

```
feature-name/

spec.md
questions.md
tasks.md
```

Future documents may include:

```
api.md
events.md
domain.md
```

if the feature becomes sufficiently complex.

---

# Source Code

Production code belongs inside the **src** directory.

```
src/
```

---

# API Project

Responsible for transport concerns.

Contains:

* Endpoints
* Middleware
* Authentication
* Authorization
* OpenAPI
* Dependency Injection

Business logic is prohibited.

---

# Application Project

Contains application use cases.

Examples:

* Commands
* Queries
* Handlers
* Validators
* DTOs
* Interfaces

Business workflows are coordinated here.

---

# Domain Project

Contains the business model.

Examples:

* Entities
* Value Objects
* Aggregates
* Domain Events
* Domain Services
* Repository Interfaces

The Domain project shall have no dependency on Infrastructure.

---

# Infrastructure Project

Contains technical implementations.

Examples:

* Dapper
* PostgreSQL
* Redis
* Azure Storage
* OpenAI
* OCR Providers
* Event Bus
* Logging

Infrastructure depends on the Domain, never the opposite.

---

# Contracts Project

Contains public contracts shared with external consumers.

Examples:

* API Models
* Event Contracts
* Shared DTOs
* Versioned Schemas

Contracts should remain stable.

---

# Vertical Slice Organization

Application code should be organized by feature instead of technical layer.

Example:

```
Application/

Features/

DocumentIngestion/
DocumentClassification/
ClauseDetection/
ClauseCategorization/
```

Each feature should contain:

```
Commands/
Queries/
Validators/
Handlers/
Models/
```

The goal is to keep each feature self-contained.

---

# Tests

Tests are separated by responsibility.

```
tests/

UnitTests/
IntegrationTests/
ContractTests/
AcceptanceTests/
```

Production code must never be placed inside the tests directory.

---

# Documentation

General documentation belongs inside:

```
docs/
```

Examples:

* ADRs
* Diagrams
* Deployment Guides
* Operational Runbooks

Feature specifications belong in `.specify/specs`, not in `docs`.

---

# Scripts

Automation scripts belong inside:

```
scripts/
```

Examples:

* Database initialization
* Local environment setup
* Code generation
* CI utilities

---

# Docker

Container-related resources belong inside:

```
docker/
```

Examples:

* Dockerfiles
* Compose fragments
* Local infrastructure

---

# Naming Conventions

Directories:

* PascalCase for .NET projects.
* kebab-case for specifications.

Examples:

```
DocumentIngestion

document-ingestion
```

Namespaces follow PascalCase.

Files should use descriptive names.

Avoid abbreviations unless they are industry standard.

---

# Dependencies

Dependencies shall follow Clean Architecture.

```
API
↓

Application
↓

Domain

Infrastructure
↑
```

Infrastructure implements interfaces defined by the Domain or Application.

The Domain never references Infrastructure.

---

# Repository Evolution

New projects may be added only when they introduce a clear architectural responsibility.

Project proliferation should be avoided.

---

# Guiding Principle

Repository organization shall prioritize business capabilities over technical concerns.

Features are the primary unit of organization.

Technology exists to support the Domain, not to define the repository structure.
