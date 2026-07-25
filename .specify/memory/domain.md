# Domain Model - Document Aggregate

**Version:** 1.0.0  
**Status:** Draft

---

# Purpose

This specification defines the Domain Model for the **Document** aggregate.

It establishes domain concepts, consistency boundaries, invariants, and relationships needed to transform a submitted file into structured business information while preserving tenant isolation and business consistency.

This document follows:

- Constitution
- Architecture
- Glossary
- Technology

---

# Ubiquitous Language Scope

This model uses glossary terms as the source of truth:

- Document
- Clause
- Clause Category
- Confidence Score
- Metadata
- Document Classification
- Structured Document
- Processing Pipeline
- Tenant
- Version

If a required concept is not yet in the glossary, the glossary must be updated before implementation.

---

# Aggregate Definition

## Aggregate Root

### Document

The **Document** is the Aggregate Root and consistency boundary.

Why it belongs in the Domain:

- It represents a core business concept: a file submitted for contractual and legal analysis.
- It owns the lifecycle from ingestion to structured representation.
- It is the natural boundary to enforce business invariants related to tenant ownership, processing state, and structured output consistency.
- Other concepts (clauses, extracted entities, metadata) have no independent business lifecycle outside a specific document context.

---

# Entities

## 1. Document (Aggregate Root)

Core identity and lifecycle owner.

Responsibilities:

- Maintain tenant ownership and isolation.
- Maintain source and classification context.
- Control transitions through processing stages.
- Guarantee consistency between raw/extracted/normalized/structured information.
- Publish domain facts when meaningful state transitions complete.

Why it belongs in the Domain:

- Lifecycle transitions reflect business meaning, not infrastructure mechanics.
- Tenant and classification constraints are business rules.
- Completion criteria for a “processable” or “structured” document are domain concerns.

## 2. Clause

A logical business statement extracted from a document.

Responsibilities:

- Preserve clause text identity within the parent document.
- Carry semantic classification and confidence.
- Support domain operations such as categorization, comparison readiness, and review readiness.

Why it belongs in the Domain:

- A clause is explicitly defined in the glossary as a business unit of analysis.
- Clause-level categorization and confidence directly affect business decisions and downstream validations.
- A clause is meaningful only within a document aggregate boundary.

## 3. ExtractedEntity

A structured business fact detected in document content (for example company, person, date, monetary value, location).

Responsibilities:

- Capture detected business entities with semantic type and confidence.
- Preserve traceability to the originating document context.
- Support business interpretation and enriched structured output.

Why it belongs in the Domain:

- Entity extraction outputs are part of the Structured Document business result.
- They influence legal/commercial interpretation, therefore they are not merely technical parser artifacts.

## 4. DocumentRevision

A versioned snapshot concept for business traceability of a Document over time.

Responsibilities:

- Represent domain version progression.
- Preserve immutable historical facts for audit and compatibility.
- Support controlled evolution when document interpretation changes.

Why it belongs in the Domain:

- Version is a glossary term and a business traceability requirement.
- Revision history enables business-level accountability and reproducibility.

Note:

- Revision storage strategy is an infrastructure concern; revision meaning and compatibility are domain concerns.

---

# Value Objects

## 1. DocumentId

Unique identity of a document within its bounded context.

Why in Domain:

- Identity semantics are business-critical for references, events, and traceability.

## 2. TenantId

Identifies the tenant owner.

Why in Domain:

- Multi-tenant isolation is a constitutional business rule and must be explicit in domain state.

## 3. DocumentSource

Origin of the document (upload, URL, cloud storage, external integration).

Why in Domain:

- Source affects business policy, validation expectations, and provenance.

## 4. DocumentClassification

Business type assigned to the document (for example NDA, Employment Contract, Service Agreement).

Why in Domain:

- Classification drives business interpretation and downstream domain behavior.

## 5. DocumentMetadata

Business-relevant descriptive information (file size, MIME type, language, page count, author, creation date).

Why in Domain:

- Metadata is part of business meaning and structured output, not only transport-level details.

## 6. NormalizedText

Canonical text representation produced before deeper semantic analysis.

Why in Domain:

- Normalization quality directly affects clause and entity business outcomes.
- It is a required intermediate domain artifact in the processing pipeline.

## 7. ClauseCategory

Primary semantic class for a clause.

Why in Domain:

- Category is a business-level concept used for policy interpretation and search/filter capabilities.

## 8. ConfidenceScore

Numeric confidence in the range $[0.0, 1.0]$.

Why in Domain:

- Confidence influences business trust thresholds and review decisions.
- Range validity is a domain invariant.

## 9. ProcessingStage

Current domain stage in the processing pipeline.

Why in Domain:

- Stage transitions represent business progression and gate valid operations.

## 10. CorrelationId

Trace identifier propagated across operations and events.

Why in Domain:

- Correlation is required by architecture/observability and part of business traceability for completed facts.

---

# Invariants

The Document aggregate must enforce at least the following invariants.

## Identity and Ownership

1. A Document must have exactly one TenantId.
2. Tenant ownership is immutable after creation.
3. All child entities (clauses, extracted entities, revisions) must belong to the same TenantId as the aggregate root.

Why in Domain:

- Tenant isolation is a mandatory business constraint, not an implementation option.

## Lifecycle and Stage Consistency

4. Stage transitions must follow an allowed order aligned to the processing pipeline.
5. A document cannot be marked as structured/completed unless required prior stages have been completed.
6. A failed stage prevents advancement until domain-defined recovery/retry rules are satisfied.

Why in Domain:

- Processing progression represents business fact maturity.

## Structural Consistency

7. Clause identifiers are unique within a document.
8. Each clause has exactly one primary category.
9. ConfidenceScore values must satisfy $0.0 \leq score \leq 1.0$.
10. Structured output cannot reference clauses or entities not owned by the document.

Why in Domain:

- These constraints preserve semantic correctness of the structured business representation.

## Version and Traceability

11. Every domain-changing revision increments document version according to approved version policy.
12. Historical revisions are immutable once finalized.
13. Domain facts emitted from aggregate transitions include document identity, tenant identity, and correlation context.

Why in Domain:

- Versioning and traceability are contract-level business concerns (compatibility, auditability, governance).

## Source and Classification Integrity

14. Document source must be one of approved business sources.
15. Classification values must be from the approved business taxonomy.

Why in Domain:

- Source and classification govern business meaning and downstream obligations.

---

# Responsibilities by Concept

## Document Aggregate Root

- Protect all invariants.
- Control creation and lifecycle transitions.
- Accept or reject clause/entity incorporation based on consistency rules.
- Govern revision/version progression.
- Emit domain events representing completed business facts.

## Clause Entity

- Preserve business statement identity and semantic category.
- Maintain confidence integrity.
- Remain valid only in context of parent document.

## ExtractedEntity Entity

- Represent extracted business facts with type and confidence.
- Preserve contextual relevance within parent document.

## DocumentRevision Entity

- Preserve immutable business snapshots over time.
- Support historical traceability and reproducibility.

---

# Relationships

## Within the Aggregate

1. Document $1 \rightarrow N$ Clause
2. Document $1 \rightarrow N$ ExtractedEntity
3. Document $1 \rightarrow N$ DocumentRevision

Relationship rationale:

- Clauses, extracted entities, and revisions derive their business meaning from one document and should not exist independently.

## To Other Domain Concepts

1. Document references exactly one Tenant (via TenantId Value Object).
2. Document may produce domain events for completed transitions (for example ingested, normalized, structured, classified).

Relationship rationale:

- Tenant context is mandatory for all operations.
- Event publication communicates completed business facts to other bounded contexts while preserving service autonomy.

---

# Domain Boundaries and Non-Domain Concerns

The following are explicitly outside the Domain Model and must remain in Infrastructure/Application layers:

- OCR provider implementation details
- AI provider SDK usage and prompt transport
- File storage adapter details (Blob/S3/local)
- SQL schema and persistence optimization
- API transport models and endpoint concerns

Rationale:

- The Domain defines business meaning and invariants; technical mechanisms are replaceable.

---

# Potential Future Extensions (Specification Candidates)

The following are identified as possible future domain evolutions and are not implemented here.

1. Multi-language document semantics
Description: language-aware normalization and clause interpretation policies.

2. Advanced document version branching
Description: parallel revision branches for legal redline workflows.

3. Clause lineage tracking
Description: explicit parent/child links between normalized and source clauses.

4. Confidence policy profiles
Description: tenant-specific confidence thresholds for auto-approval vs manual review.

5. ABAC-ready authorization attributes
Description: richer domain attributes to support future ABAC policy evaluation.

6. Document-level risk scoring
Description: aggregate business risk indicator derived from classified clauses.

7. Cross-document semantic linking
Description: controlled references between documents in the same tenant context.

Each extension requires:

- Glossary updates (if new terms are introduced)
- Domain model update
- Event and API contract update where applicable
- Approval before implementation

---

# Open Clarifications Required Before Implementation

To fully finalize implementation-facing specifications, the following must be explicitly approved in related specifications:

1. Canonical list of allowed DocumentClassification values.
2. Canonical taxonomy for ClauseCategory values.
3. Exact state machine for ProcessingStage and failure/retry transitions.
4. Document revision/version increment policy.
5. Domain event names and versioning policy for document lifecycle facts.

Until these are approved, implementation must not infer missing business behavior.

---

# Guiding Principle

The Document aggregate exists to protect business consistency while converting unstructured input into trusted structured knowledge, always within strict tenant boundaries and explicit domain rules.