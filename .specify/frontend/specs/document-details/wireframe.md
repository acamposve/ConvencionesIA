# Document Details Feature Wireframe

## Layout Overview

The document-details screen should be a single, focused content panel with clear sections and minimal clutter.

## Suggested Structure

1. Header
   - Title: Document detail
   - Document reference or title
   - Source and document identifier

2. Status Row
   - Processing stage chip
   - Outcome chip

3. Metadata Section
   - Tenant
   - Correlation ID
   - Format
   - MIME type

4. Processing Insight Section
   - State
   - Detected document type
   - Language

5. Optional Enrichment Section
   - Summary text
   - Classification row with code and confidence

## Visual Notes

- Use card-based layout for clarity.
- Use chips for lifecycle status and outcome.
- Use horizontal spacing to separate metadata and insight sections.
- Keep the page readable without excessive scrolling.

## State Variations

- Loading: show skeleton placeholders.
- Error: show an inline alert with guidance.
- Success: show the full content sections.
- Empty: show neutral messaging when optional enrichment values are absent.
