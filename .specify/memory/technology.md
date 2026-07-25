# Technology

**Version:** 1.0.0

**Status:** Draft

---

# Purpose

This document defines the approved technology stack for the platform.

It establishes the technologies, frameworks, libraries, and engineering standards that every service shall follow.

Technology choices must remain aligned across all repositories unless an approved Architectural Decision Record (ADR) specifies otherwise.

---

# Architecture

The platform follows a cloud-native microservices architecture based on:

* Domain-Driven Design (DDD)
* Clean Architecture
* Vertical Slice Architecture
* CQRS
* Event-Driven Architecture
* API-First Design

---

# Backend

## Language

C#

Version:

.NET 10

---

## Framework

ASP.NET Core Minimal APIs

Controllers are not used unless explicitly justified.

---

## API Documentation

OpenAPI 3.x

Swagger UI shall be enabled for development environments.

---

## Validation

FluentValidation

Validation shall execute before business logic.

---

## Object Mapping

Manual mapping is preferred.

Automatic mapping libraries should be avoided unless approved.

---

# Data Access

ORMs are intentionally avoided.

Preferred technology:

Dapper

Raw SQL is acceptable when it improves performance or readability.

Repositories shall encapsulate persistence logic.

---

# Database

Primary database:

PostgreSQL

Each microservice owns its own database.

Cross-service database access is prohibited.

---

# Caching

Redis

Caching strategies shall be defined per feature.

---

# Messaging

Asynchronous communication shall use an event broker.

Preferred technologies include:

* Azure Service Bus
* RabbitMQ
* Kafka

The specific broker may vary by deployment environment.

---

# AI Integration

AI providers are replaceable infrastructure components.

Supported providers may include:

* Azure OpenAI
* OpenAI
* Anthropic
* Google Gemini

Business logic shall never depend directly on a specific provider.

Provider-specific implementations must remain inside Infrastructure.

---

# Embeddings

Embeddings shall be generated through the configured AI provider.

Embedding models must be configurable.

Embeddings shall never be hardcoded to a specific vendor.

---

# OCR

OCR providers shall be abstracted.

Possible implementations include:

* Azure AI Vision
* Tesseract
* Google Vision

---

# Storage

Supported document storage providers include:

* Local Storage (Development)
* Azure Blob Storage
* Amazon S3
* Google Cloud Storage

Storage implementations must be interchangeable.

---

# Authentication

Preferred protocol:

OAuth 2.1

Token format:

JWT

Identity implementation may be provided by:

* Internal Identity Service
* Microsoft Entra ID
* Auth0
* Keycloak

Business services shall depend only on authentication abstractions.

---

# Authorization

Authorization shall be claims-based.

Role-Based Access Control (RBAC) is the default authorization strategy.

Future support for Attribute-Based Access Control (ABAC) is expected.

---

# Observability

Logging

Serilog

Metrics

OpenTelemetry

Tracing

OpenTelemetry

Health Checks

ASP.NET Core Health Checks

Every request shall include a Correlation Id.

---

# Testing

Unit Tests

xUnit

Mocking

NSubstitute

Assertions

FluentAssertions

Integration Tests

ASP.NET Core Test Host

Contract Tests

OpenAPI Contract Validation

Acceptance Tests

Defined per Feature Specification.

---

# Dependency Injection

The built-in .NET dependency injection container shall be used.

External containers are discouraged.

---

# Configuration

Application configuration shall be loaded from:

* appsettings.json
* Environment Variables
* Azure Key Vault (Production)

Secrets shall never be committed to source control.

---

# Containerization

Docker is mandatory.

Every service shall provide:

* Dockerfile
* .dockerignore

---

# Orchestration

Development

.NET Aspire

Production

Container orchestration platform to be defined by deployment environment.

Examples include:

* Azure Container Apps
* Kubernetes

---

# CI/CD

Build pipeline shall include:

* Restore
* Build
* Static Analysis
* Unit Tests
* Integration Tests
* Security Scan
* Artifact Generation

Deployment shall occur only after successful quality gates.

---

# Coding Standards

Every implementation shall follow:

* SOLID Principles
* Clean Code
* Dependency Inversion
* Asynchronous Programming
* Nullable Reference Types
* Immutable Value Objects

Warnings shall be treated as build failures whenever practical.

---

# Version Control

Git

Main branch shall always remain deployable.

Feature development shall occur in isolated branches.

---

# Future Technology Changes

Technology choices may evolve.

Business rules, domain model, and specifications must remain independent of technology decisions.

Any significant technology change requires an approved ADR.

---

# Guiding Principle

Technology exists to support the Domain.

No technology decision shall compromise the Domain Model, Architectural Principles, or the Constitution.
