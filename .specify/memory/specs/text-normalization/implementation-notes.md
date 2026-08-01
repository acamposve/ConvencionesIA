# Text Normalization Implementation Notes

## Scope confirmation

The implementation remains within the approved OCR-only normalization scope. It does not introduce structural reconstruction, semantic rewriting, or downstream AI behavior.

## Implemented behavior

- Added a normalization contract and OCR-focused normalization service.
- Extended the Document aggregate with NormalizedText state and lifecycle enforcement.
- Added a normalization use case that records normalized text and transitions the document to Failed on normalization failure.
- Wired normalization into the extraction workflow so it runs after extracted text is available.
- Published success and failure events without exposing document content.
- Persisted normalized text through the repository layer so it survives save and reload operations.
- Added structured logs that record document and correlation context without logging the content itself.
- Added unit and integration tests covering success paths, failure paths, event publication, state transitions, and persistence.

## Assumptions and review notes

- Normalization is deterministic and limited to OCR artifact cleanup such as line endings, whitespace, quotes, dash variants, and non-printable characters.
- The original extracted text remains available and unchanged after normalization.
- The feature is ready for review or handoff as an OCR-focused text-cleanup step for downstream processing.
