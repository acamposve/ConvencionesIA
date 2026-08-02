# Document List Feature Domain

## Overview

The document list is the primary review experience for the document-ingestion platform. It allows users to browse the current inventory of documents, understand their lifecycle state, and navigate into a selected document for deeper inspection.

## Core Domain Concepts

### Document Inventory
The document list represents the current set of documents available through the public API contract.

### Document Lifecycle
The list must reflect the shared lifecycle model used across the product:
- PendingProcessing
- Accepted
- Rejected
- Failed
- Completed

### Intake Mode
The list should surface whether a document entered through:
- Upload
- URL

### Review Intent
The document list exists to support the user’s need to review, scan, and select documents for inspection.

## Domain Rules

1. The document list must use data from the public API contract and not from implementation details.
2. Document state must be presented consistently with the rest of the product experience.
3. The document list must be usable in a no-auth demo environment.
4. The list must help users identify documents that require attention without forcing extra navigation.
5. The document list should expose enough information to make selection easy but remain compact and scannable.

## User Intent Model

The document list supports three primary intents:
- review available documents
- identify a document to inspect
- understand current lifecycle state at a glance

## Design Implications

- The list should be scan-friendly and lightweight.
- Status presentation should be consistent with the rest of the platform.
- Each item should support quick inspection and navigation.
- The experience should feel like a document inventory rather than a technical debug screen.
