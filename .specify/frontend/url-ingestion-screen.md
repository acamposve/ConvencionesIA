# URL Ingestion Screen

## Purpose

Provide a first-class screen for submitting a document to the platform by URL. This flow must be treated as a primary intake path, not a secondary fallback.

## User Story

As a business user, I want to submit a document by pasting a public URL so that the platform can ingest it and show me its processing status.

## Screen Requirements

- A dedicated route at /documents/url
- A single URL input field with validation and helper text
- Clear loading, success, and error states
- A submit action that creates an ingestion request and transitions the user into the shared document lifecycle view
- The screen must reuse the same document detail and status experience as file upload

## Core Content Areas

1. Header
   - Title: Ingest document from URL
   - Short description of the expected workflow
2. Form
   - URL input
   - Optional tenant context selector or prefilled tenant context
   - Submit button
3. Validation State
   - Invalid URL feedback
   - Network or accessibility guidance when the URL cannot be reached
4. Submission Result
   - Confirmation of ingestion acceptance
   - Link or redirect to the new document detail page

## Acceptance Criteria

- A user can enter a valid URL and submit it successfully.
- Invalid or empty URLs show actionable validation messages.
- Successful submission surfaces the same processing-state experience as file upload.
- The screen is accessible, responsive, and uses the shared design system.
