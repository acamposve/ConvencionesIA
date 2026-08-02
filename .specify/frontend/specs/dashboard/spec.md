# Dashboard Feature Specification

**Feature:** Dashboard
**Version:** 1.0.0
**Status:** Draft

---

## Purpose

Define the MVP dashboard experience for the document-ingestion platform so business users can quickly understand recent activity, initiate ingestion, and inspect the lifecycle of documents without needing backend tooling.

This specification is product- and experience-focused. It defines the required user experience and behavior for the dashboard feature, while leaving implementation details to the engineering team.

---

## Problem Statement

Users need a simple entry point into the document-ingestion workflow that shows what has happened recently, highlights documents that require attention, and enables fast navigation to the most relevant document actions.

The current product experience is backend-oriented and lacks a clear front-door experience for demos, stakeholder reviews, and operational visibility.

---

## Goals

- Give users a clear overview of recent ingestion activity.
- Make the document lifecycle visible at a glance.
- Provide fast paths to ingest a new document through upload or URL.
- Enable quick navigation to document details and status inspection.
- Support a demo-friendly, no-auth MVP experience.

---

## Non-Goals

- Full administration or settings management.
- Complex analytics or reporting dashboards.
- Authentication, role management, or multi-user collaboration flows.
- Deep workflow automation beyond the current ingestion lifecycle.

---

## User Personas

### Primary Persona: Business User
A user who wants to submit documents, monitor progress, and confirm outcomes without interacting with APIs or infrastructure.

### Secondary Persona: Demo Presenter
A user who needs a polished, simple experience to show the product’s value during a live presentation or stakeholder review.

---

## Core User Flows

### 1. Start a new ingestion
A user can begin a new ingestion from the dashboard using either:
- a file upload action
- a URL-based ingestion action

### 2. Review recent activity
A user can see recent documents and their current status, including pending, accepted, rejected, failed, or completed states.

### 3. Inspect a document
A user can open a document to view its metadata, outcome, lifecycle stage, and any available summaries or classification information.

### 4. Recover from issues
A user can identify failed or rejected documents and navigate to the detail view for more context or retry guidance.

---

## Functional Requirements

### FR-1 Dashboard Entry Point
The dashboard must be the primary landing page for the MVP experience and provide clear navigation to ingestion, document list, and document detail views.

### FR-2 Quick Actions
The dashboard must expose prominent actions for:
- uploading a document
- ingesting a document by URL
- viewing the document list

### FR-3 Activity Summary
The dashboard must present a concise summary of recent ingestion activity, including at minimum:
- total documents seen
- documents in progress
- documents completed
- documents failed or rejected

### FR-4 Recent Documents
The dashboard must display a recent documents section showing a list of the latest documents with:
- document name or source reference
- current lifecycle stage
- outcome status
- intake mode
- timestamp

### FR-5 Status Visibility
The dashboard must clearly communicate lifecycle states using a consistent visual pattern across cards, tables, and detail views.

### FR-6 Navigation to Detail
Each recent document item must be actionable and allow the user to navigate to the document detail experience.

### FR-7 Empty and Error States
If there are no documents or if the data cannot be loaded, the dashboard must show an actionable empty or error state that helps the user continue.

### FR-8 Responsive Layout
The dashboard must remain usable on desktop, tablet, and mobile, preserving the primary actions and summary information at each breakpoint.

### FR-9 Accessibility
The dashboard must meet WCAG AA expectations for keyboard navigation, focus management, ARIA labels, and screen-reader announcements for status changes and errors.

### FR-10 Demo-Friendly Experience
The dashboard must support a no-auth demo experience and avoid introducing login or account management steps in the MVP.

---

## UX Requirements

### Content Structure
The dashboard should include the following sections in the main viewport:
1. Header with title and primary actions
2. Summary cards for document activity
3. Recent documents list or table
4. Secondary guidance for next steps

### Visual Guidance
- Use the shared design system and official palette.
- Status chips should be used for lifecycle states.
- Cards should be used for summary sections.
- Prefer concise, business-friendly labels over technical implementation language.

### Interaction Guidance
- Primary actions must be visible above the fold on desktop.
- The layout should support quick scanning and avoid unnecessary scrolling for core information.
- Loading states must appear while data is refreshing.

---

## Data Requirements

The dashboard must consume the public API contract for the MVP:
- POST /api/v1/documents/ingestion
- GET /api/v1/documents/{id}
- GET /api/v1/documents

The UI should present data derived from these endpoints, including:
- document lifecycle stage
- outcome
- source/intake mode
- metadata such as tenant, correlation identifier, and timestamp
- summary and classification information when available

---

## Acceptance Criteria

1. Given a user opens the dashboard, they can see summary cards for recent ingestion activity.
2. Given a user wants to start a new ingestion, they can access upload and URL-based actions from the dashboard.
3. Given documents exist in the system, the dashboard shows a recent documents list with status and source information.
4. Given a document is selected, the user can navigate to the detail view for further inspection.
5. Given the data source is unavailable, the dashboard shows a clear error state with a retry path.
6. Given the dashboard is viewed on a smaller screen, the core actions and summary remain usable.

---

## Constraints

- The implementation must use the approved frontend stack.
- The feature must follow the frontend constitution and use shared components rather than page-local business logic.
- The implementation must not introduce authentication flows in the MVP.
- The feature must align with the existing document lifecycle model and public API contract.

---

## Open Questions

1. Should the dashboard include a “needs attention” section for failed or rejected documents?
2. Should the recent documents list be paginated or limited to a fixed number of items in the MVP?
3. Should the dashboard show a small trend view for ingestion volume over time?

---

## Definition of Done

The dashboard feature is considered complete when:
- the dashboard screen exists and is reachable through the documented route structure
- the summary cards, recent documents list, and primary actions are implemented
- the feature is responsive and accessible
- the experience works against the thin MVP API contract
- the feature includes the required tests for unit, component, and integration coverage
