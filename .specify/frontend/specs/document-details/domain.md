# Document Details Feature Domain

## Domain Summary

The document-details feature is a read-only inspection experience for a single document in the ingestion workflow. It focuses on presenting the current document state and the most relevant metadata and enrichment information gathered during processing.

## Core Concepts

- Document: the entity being inspected.
- Processing Stage: the current lifecycle state of the document.
- Outcome: the current result for the document, such as pending, completed, or failed.
- Metadata: informational fields describing the document and its processing context.
- Processing Insight: derived information about the document, such as detected type, language, summary, or classification.

## Key Behaviors

- The detail view loads a document by identifier from the route.
- The view shows status and outcome prominently.
- The view surfaces metadata and processing insight in separate sections.
- Optional enrichment sections appear only when content exists.
- The view must remain stable during loading and provide a clear error state if the data cannot be retrieved.

## Domain Rules

- A document detail view must always be anchored to a specific document identifier.
- The page should reflect the latest known lifecycle and enrichment state from the API.
- If expected enrichment data is not present, the UI should degrade gracefully rather than fail the page.
- The feature should remain focused on inspection rather than editing or workflow actions.

## User-Facing Information Model

The screen is expected to present the following information:

- title or source reference
- source and identifier
- processing stage
- outcome
- tenant identifier
- correlation identifier
- format
- MIME type
- state
- detected document type
- language
- summary text when available
- classification code and confidence when available
