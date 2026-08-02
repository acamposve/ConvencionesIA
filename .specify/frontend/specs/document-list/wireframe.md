# Document List Wireframe

## Layout Overview

The document list should be a single-page experience with the following structure:

1. Header
   - Title: Documents
   - Subtitle: Review documents that have entered the ingestion workflow
   - Primary action: New upload

2. Document Inventory Section
   - A list or table with rows for each document
   - Each row includes:
     - document name or source reference
     - lifecycle state
     - intake mode
     - unique identifier

3. Supporting States
   - Loading skeletons while the data is being fetched
   - Empty state when no documents exist
   - Error state when the data cannot be loaded

## Interaction Notes

- Each list item should be clickable and navigate to the document detail experience.
- Status badges should use the shared design system palette.
- The layout should stack gracefully on smaller viewports.
- The page should feel lightweight and easy to scan.
