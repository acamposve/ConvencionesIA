# Dashboard Wireframe

## Layout Overview

The dashboard should be a single-page experience with the following structure:

1. Header
   - Title: Dashboard
   - Subtitle: Monitor ingestion activity and start new processing flows
   - Primary actions: Upload document, Ingest from URL

2. Summary Section
   - Summary cards for:
     - Total documents
     - In progress
     - Completed
     - Failed or rejected

3. Recent Documents Section
   - A list or table with columns for:
     - Document / source
     - Status
     - Intake mode
     - Updated time
   - Each row is clickable and navigates to the document detail view.

4. Supporting Guidance
   - Short text encouraging the user to inspect a document or start a new ingestion.
   - Empty state messaging when no documents are available.

## Interaction Notes

- The primary actions should be visually prominent.
- Status chips should use the shared design system color palette.
- The layout should stack gracefully on smaller viewports.
- The page should provide a clear loading skeleton before content appears.
