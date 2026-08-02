# UI Guidelines

- Responsive by default.
- Skeleton while loading.
- Empty and error states.
- Breadcrumbs.
- Accessible (WCAG AA).
- Confirmation for destructive actions.
- Show the document lifecycle clearly: pending, processing, accepted, rejected, failed, and completed.
- Support both ingestion entry points: file upload and URL-based ingestion.
- Provide a dedicated URL ingestion screen with a focused form, validation feedback, and a clear handoff to the shared document lifecycle experience.
- Keep the MVP demo-first and no-auth; the UI should present a straightforward flow for submitting documents without introducing authentication or account management complexity.
- Use a single, consistent status pattern across tables, cards, and detail views.
- Present document metadata in a structured layout: tenant, correlation id, upload time, current stage, outcome, and intake mode.
- Highlight classification, summary, and embedding information as primary content when available.
- Keep long-running processing visible with progress feedback and a retry or refresh path.
- Empty states should explain what the user can do next, such as upload a document or refine the search.
- Error states should be actionable and should explain whether the issue is recoverable or requires support.
