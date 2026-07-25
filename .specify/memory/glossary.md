# Glossary

**Version:** 1.0.0

**Status:** Draft

---

# Purpose

This glossary defines the ubiquitous language used throughout the platform.

All specifications, source code, documentation, APIs, prompts, and AI-generated artifacts SHALL use these terms consistently.

If a new business concept is introduced, this glossary MUST be updated before implementation.

---

# A

## Aggregate

A consistency boundary within the Domain Model responsible for enforcing business invariants.

---

## API

A public contract exposed by a service for external or internal consumers.

---

## AI Provider

An external service capable of performing AI tasks such as clause extraction, summarization, classification, or embeddings.

Examples include OpenAI, Azure OpenAI, Anthropic, and Google Gemini.

---

# C

## Category

A business classification assigned to a Clause.

Examples:

* Payment
* Confidentiality
* Liability
* Warranty
* Intellectual Property

---

## Clause

A logical business statement extracted from a document.

A Clause represents the smallest unit that can be independently analyzed, categorized, compared, searched, or validated.

---

## Clause Category

The semantic classification assigned to a Clause.

Each Clause should belong to one primary category.

---

## Confidence Score

A numeric value indicating the confidence of an AI prediction.

Confidence ranges from 0.0 to 1.0.

---

## Command

A request that modifies the state of the system.

Commands never represent queries.

---

## Correlation Id

A unique identifier used to trace a request across multiple services.

---

# D

## Document

A file submitted to the platform for analysis.

A Document may originate from:

* Upload
* URL
* Cloud Storage
* External Integration

Supported formats include PDF, Word documents, and images.

---

## Document Classification

The business type assigned to a Document.

Examples include:

* NDA
* Employment Contract
* Service Agreement
* Purchase Agreement
* Lease Agreement

---

## Document Intelligence

The capability of transforming an unstructured document into structured business information.

---

## Document Source

The origin from which a document is obtained.

---

## Domain Event

An event representing a completed business fact within a domain.

---

# E

## Embedding

A numeric vector representing the semantic meaning of a Clause or Document.

Embeddings enable semantic search, similarity detection, and AI-powered comparison.

---

## Entity Extraction

The process of identifying structured business entities within a document.

Examples include:

* Companies
* Dates
* Monetary values
* Persons
* Locations

---

## Event

An immutable business fact published after successful completion of a business operation.

---

# F

## Feature

An independently specified business capability.

Each Feature owns its own specification, implementation, tests, and acceptance criteria.

---

# I

## Ingestion

The process of receiving and validating a document before analysis.

---

# M

## Metadata

Information describing a document rather than its content.

Examples include:

* File Size
* MIME Type
* Language
* Number of Pages
* Author
* Creation Date

---

## Multi-Tenant

An architectural model where multiple organizations share the same platform while remaining completely isolated.

---

# N

## Normalized Clause

A Clause whose wording has been standardized while preserving its original meaning.

---

## Normalized Text

Text that has been cleaned and standardized prior to AI processing.

Normalization may include:

* Removing headers
* Removing footers
* Fixing whitespace
* Unicode normalization
* Line merging

---

# O

## OCR

Optical Character Recognition.

The process of extracting text from images or scanned documents.

---

# P

## Processing Pipeline

The ordered sequence of operations performed on a Document.

Typical stages include:

1. Ingestion
2. Detection
3. Extraction
4. Normalization
5. Clause Detection
6. Categorization
7. AI Enrichment

---

# Q

## Query

A request that retrieves information without modifying system state.

---

# R

## Repository

An abstraction responsible for persisting and retrieving Aggregate Roots.

Repositories belong to the Domain but are implemented by Infrastructure.

---

# S

## Service

An independently deployable application responsible for a single bounded context.

---

## Structured Document

The final structured representation produced after document processing.

A Structured Document contains:

* Metadata
* Extracted Text
* Clauses
* Categories
* Entities
* Confidence Scores
* AI Enrichment

---

# T

## Tenant

An organization that owns data within the platform.

Every business operation executes within a Tenant context.

---

## Text Extraction

The process of obtaining textual content from a supported document.

---

# U

## Ubiquitous Language

The shared vocabulary used by business stakeholders, developers, architects, testers, and AI assistants.

Every specification shall use the terms defined in this glossary.

---

# V

## Vector Search

A search technique based on semantic similarity using Embeddings instead of keyword matching.

---

# Version

The revision identifier of a Document, API, Event, or Specification.

Versioning enables backward compatibility and traceability.

---

# Guiding Principle

If a business term is ambiguous, undefined, or inconsistently used, implementation SHALL stop until the glossary has been updated and approved.
