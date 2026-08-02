# URL Ingestion Feature Domain

## Overview

The URL-ingestion experience is the alternative entry point for submitting a document into the document-ingestion workflow through a public URL. It ensures the user can provide the necessary context, submit the URL, and move into the shared lifecycle experience.

## Core Domain Concepts

### URL Intake Intent
The user wants to submit a document through a URL and receive confirmation that it entered the workflow.

### Submission Context
The URL-ingestion flow requires the user-supplied submission details needed for ingestion, including source reference and URL identity. Tenant context and correlation context are inherited from the signed-in session.

### Submission Outcome
The experience must clearly communicate whether the submission was accepted, is still in progress, or failed.

### Recovery Guidance
When a submission fails, the experience must help the user understand the issue and recover with a retry.

## Domain Rules

1. The URL-ingestion experience must use the public API contract and not reimplement backend validation logic.
2. The flow must be consistent with the shared document lifecycle model used elsewhere in the product.
3. The experience must work in a no-auth demo environment.
4. The UI must communicate success and failure clearly without requiring technical knowledge.
5. The flow should feel lightweight and focused on the next action.

## User Intent Model

The URL-ingestion flow supports three primary intents:
- initiate a document submission via URL
- receive feedback about the submission outcome
- continue into the document lifecycle experience

## Design Implications

- The form should be concise and easy to complete.
- Success and error states should be clearly distinguishable.
- The experience should guide the user toward the next meaningful step after submission.
- The UI should not expose internal implementation complexity to business users.
