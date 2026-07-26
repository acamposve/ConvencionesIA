# Domain Model

**Feature:** Document Type Detection

**Version:** 1.0.0

**Status:** Draft

---

# Purpose

This document defines the Domain Model for the Document Type Detection feature.

It describes the business concepts involved, their responsibilities, invariants, and relationships.

Implementation details, libraries, and detection algorithms are intentionally excluded.

---

# Aggregate Root

## Document

The Document Aggregate is responsible for maintaining the lifecycle of an ingested document.

Document Type Detection enriches the Document by determining its actual document type.

The detected type becomes part of the Aggregate state.

---

# Entities

This feature introduces no new Entities.

The existing Document Aggregate is updated.

---

# Value Objects

## DocumentType

Represents the detected type of a document.

Possible values include:

- Pdf
- Doc
- Docx
- Png
- Jpeg
- Tiff
- Unknown

### Responsibilities

- Represent the detected document type.
- Drive downstream processing.
- Prevent unsupported processing paths.

### Invariants

- Every Document has exactly one DocumentType.
- Unknown is only valid when detection fails.
- The value cannot be null.

---

# Aggregate State Changes

Before detection

Document

- Status = Received
- DocumentType = Unknown

After successful detection

Document

- Status = TypeDetected
- DocumentType = Pdf | Docx | Png | ...

After failure

Document

- Status = Failed
- DocumentType = Unknown

---

# Business Invariants

## INV-001

The detected document type is immutable for the lifetime of a processing job.

---

## INV-002

The detected document type shall be the source of truth.

---

## INV-003

The file extension shall never override the detected document type.

---

## INV-004

Unsupported document types shall never continue through the processing pipeline.

---

# Domain Behaviors

The Document Aggregate shall support the following behaviors.

## DetectDocumentType()

Determines the actual document type.

Preconditions

- Document exists.
- Document has a valid file.
- Status = Received.

Postconditions

- DocumentType assigned.
- Status updated.
- Domain Event published.

---

## RejectUnsupportedType()

Marks the document as rejected.

Preconditions

- Document type detected.
- Type unsupported.

Postconditions

- Status = Failed.
- Processing stops.
- Failure Event published.

---

# Domain Events

## DocumentTypeDetected

Published when document type detection succeeds.

Properties

- DocumentId
- TenantId
- DocumentType
- DetectedAt
- CorrelationId

---

## DocumentTypeDetectionFailed

Published when detection fails.

Properties

- DocumentId
- TenantId
- FailureReason
- CorrelationId

---

# Relationships

```
Document
    │
    └── DocumentType (Value Object)
```

No additional relationships are introduced.

---

# State Transitions

```
Received
    │
    ▼
TypeDetected
    │
    ▼
ReadyForTextExtraction
```

Failure path

```
Received
    │
    ▼
Failed
```

---

# Consistency Rules

- DocumentType shall be assigned exactly once during a processing job.
- Detection shall not modify the document contents.
- Detection shall not modify metadata unrelated to document type.
- Every successful detection shall produce a DocumentTypeDetected event.

---

# Future Evolution

Future versions may support additional document types, including:

- RTF
- HTML
- EPUB
- ODT
- XLSX
- PPTX

The Document Aggregate shall remain unchanged when new document types are introduced.

Only the DocumentType Value Object will evolve.