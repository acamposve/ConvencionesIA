Frontend Constitution

Purpose

This constitution defines the mandatory engineering principles for the frontend of the document-ingestion platform.

Article I --- User Experience First

Every screen MUST be designed around the user's workflow rather than backend capabilities. For this product, the primary workflows are document upload, ingestion review, status monitoring, and document inspection.

Article II --- Component First

Business logic SHALL NEVER be duplicated inside pages. Reusable UI belongs to shared components. Business workflows belong to features. The document-processing domain should be expressed in feature modules such as upload, documents, inspection, and administration.

Article III --- API First

The frontend SHALL consume only public APIs. Screens should prefer aggregated endpoints designed for UI needs. The UI must not reimplement backend validation or processing rules.

For the MVP, the frontend MUST target the thin document-ingestion API surface and rely on the following public endpoints:
- POST /api/v1/documents/ingestion
- GET /api/v1/documents/{id}
- GET /api/v1/documents

The demo experience is intentionally no-auth. The UI should assume demo tenant and user context are supplied through the supported demo headers rather than introducing authentication flows.

Article IV --- Responsive by Default

All screens MUST support desktop, tablet, and mobile. Core workflows must remain usable at each breakpoint without loss of critical information.

Article V --- Accessibility

Minimum WCAG AA. Keyboard navigation, focus indicators, ARIA labels, and sufficient contrast are required. Status, progress, and error states must be announced clearly to assistive technologies.

Article VI --- Performance

Lazy load routes. Cache server state with TanStack Query. Virtualize large tables when necessary. Display skeletons while loading. Avoid unnecessary re-renders. Poll processing status with backoff and cancellation awareness.

Article VII --- User Feedback

Every long-running action shows progress. Every error is actionable. Destructive actions require confirmation. Uploads, processing, and retries must expose clear progress and recovery instructions.

Article VIII --- Consistency

Use the Design System only. No inline styles. No duplicated components. Use the shared theme and shared status patterns across the application.

Article IX --- State Management

Server state: TanStack Query. Form state: React Hook Form. Validation: Zod. Local UI state: React. Avoid global state unless required.

Article X --- Testing

Every feature includes unit tests, component tests, integration tests, and end-to-end tests. For this product, tests must cover upload validation, status polling, document detail rendering, error handling, and retry behavior.

Article XI --- AI-Driven Development

Every feature MUST contain spec.md, domain.md, tasks.md, and wireframe.md. Implementation MUST NOT precede specification.

Article XII --- Domain-Specific Workflow Guidance

The frontend MUST support the document lifecycle clearly: accepted, pending processing, failed, rejected, and completed states. The UI must communicate document classification, extraction status, summaries, and embedding readiness in a way that is understandable to business users.

Article XIII --- Intake Modes

The frontend MUST support two ingestion entry points: file upload and URL-based ingestion. Both flows must converge into the same document lifecycle experience, including status tracking, outcome display, and inspection views. The UI should expose the intake mode clearly in the document detail and history views, and errors must be tailored to the selected mode (for example, file-size or file-type errors for uploads, and network or fetch errors for URL ingestion).

Approved Technology Stack

React 19
TypeScript
Vite
Material UI
TanStack Query
React Router
React Hook Form
Zod
Axios
Vitest
Playwright

Official Color Palette

Primary: #1E3A5F
Secondary: #2F80ED
Accent: #00B8D9
Success: #2E7D32
Warning: #ED6C02
Error: #D32F2F
Background: #F6F8FB
Surface: #FFFFFF

Typography: Inter
Icons: Lucide
Border radius: 8px