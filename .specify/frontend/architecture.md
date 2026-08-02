# Frontend Architecture

## Principles
- Feature-first structure.
- Shared components.
- Business logic in features.
- Single SPA.
- Lazy-loaded routes.
- The UI should mirror the backend document lifecycle and processing stages.

## Recommended project structure
- src/app
  - routes
  - providers
  - theme
- src/features
  - documents
    - list
    - detail
    - upload
    - status
  - inspection
    - summary
    - classification
    - embeddings
- src/shared
  - components
  - hooks
  - api
  - utils
  - types

## Recommended route map
- /dashboard
- /documents
- /documents/upload
- /documents/url
- /documents/:id
- /documents/:id/summary
- /search
- /administration
- /settings

## Architectural responsibilities
- The API layer should isolate HTTP calls and DTO mapping.
- Feature modules should own screens, forms, and data-fetching hooks.
- Shared components should be used for cards, tables, dialogs, empty states, and status chips.
- TanStack Query should be used for server-state loading, cache, and polling.

## MVP API contract expectations
- The frontend should target the thin API host rather than any internal backend service directly.
- The primary UI contract is:
  - POST /api/v1/documents/ingestion for submission
  - GET /api/v1/documents/{id} for detail retrieval
  - GET /api/v1/documents for listing and pagination
- The MVP is demo-first and no-auth; the UI should not assume a login flow is required to exercise the core experience.

## Document workflow expectations
- The upload screen should submit a document and immediately show pending state.
- The URL-ingestion screen should support entering a document URL and immediately show pending state.
- Both intake paths should converge into the same document lifecycle experience and reuse the same document detail and status components.
- The document detail screen should present outcome, stage, classification, summary, and embedding state.
- Polling should be used for processing state updates, with graceful fallback when the backend is unavailable.
- Error states should be actionable and show a retry path where appropriate.
