# Document Processing Feature Wireframe

## Layout Overview

The document-processing experience is represented through a compact status and insight view that can appear in dashboard cards and detail panels.

## Suggested Structure

1. Status Summary
   - Processing stage chip
   - Outcome chip

2. Insight Section
   - Summary text
   - Classification row with code and confidence

3. Supporting State
   - Loading skeletons
   - Inline error message

## Visual Notes

- Use clear status colors for pending, completed, and failed states.
- Keep the information compact and scannable.
- Place the most important status information first.
- Use card or panel spacing to organize the content.

## State Variations

- Loading: show skeleton placeholders.
- Error: show an inline alert with recovery guidance.
- Success: show status summary and insight content.
