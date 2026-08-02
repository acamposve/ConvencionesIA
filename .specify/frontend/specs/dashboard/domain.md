# Dashboard Feature Domain

## Overview

The dashboard is the entry experience for the document-ingestion platform. It is responsible for presenting a high-level view of ingestion activity, surfacing recent documents, and guiding the user toward the next best action.

## Core Domain Concepts

### Document Lifecycle
The dashboard must reflect the shared document lifecycle model:
- PendingProcessing
- Accepted
- Rejected
- Failed
- Completed

### Intake Modes
The dashboard must distinguish between the two supported ingestion entry points:
- Upload
- URL

### Activity Summary
The dashboard summarizes the current state of the document set into meaningful buckets that help a user understand workload and health.

### Navigation Intent
The dashboard acts as a hub that connects the user to:
- ingestion entry points
- document list views
- document detail views

## Domain Rules

1. The dashboard must present data from the public API contract and not from internal implementation details.
2. The dashboard must be consistent with the document lifecycle model used elsewhere in the product.
3. The dashboard must not introduce authentication or account concepts in the MVP.
4. The dashboard must make the most important status information visible without requiring deep navigation.
5. The dashboard must treat failed and rejected documents as actionable items that can be inspected further.

## User Intent Model

The dashboard supports four primary intents:
- start a new ingestion
- review recent activity
- inspect a document
- recover from a failed or rejected state

## Design Implications

- Summary cards should be easy to scan and interpret.
- Recent documents should be sorted by recency.
- Status presentation should be consistent with the rest of the platform.
- The dashboard should feel like a command center rather than a generic analytics page.
