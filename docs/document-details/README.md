# Document Details Feature

## Overview

The Document Details feature provides a focused view of an individual document that has entered the ingestion workflow. It is designed to help users inspect the document’s current processing state, key metadata, and any available processing insights without leaving the application.

## User experience

The feature is available from the documents area through the document detail route:

- /documents/:id

When a user opens a document from the inventory, the detail page displays:

- the document title or source reference
- the document source and identifier
- the current processing stage and outcome
- metadata such as tenant, correlation ID, format, and MIME type
- processing insight fields such as state, detected document type, and language
- optional summary and classification information when those values are present

## What the page shows

### Header and status

The page renders a prominent heading, "Document detail", and surfaces the current lifecycle status using chips for:

- processing stage
- outcome

### Metadata section

The metadata section includes:

- tenant identifier
- correlation identifier
- document format
- MIME type

### Processing insight section

The processing insight section includes:

- current state
- detected document type
- language

### Optional content

If the related data exists, the page can also display:

- a document summary
- a classification entry with its code and confidence score

## Data flow

The page uses the document identifier from the route and retrieves the corresponding document via the shared document API client. The request is handled through a dedicated hook that loads the document by ID and exposes loading, error, and success states.

## Technical notes

The feature is implemented as a route-driven page in the frontend and is currently backed by the shared document API layer. It follows the current frontend pattern of:

- route-based navigation
- asynchronous data loading
- loading and error states
- presentation through Material UI components

## Testing

The feature has a basic UI test that verifies the page renders its heading when the detail route is visited.

## Current scope

This feature is a read-only detail experience for inspecting a single document. It does not currently provide editing or workflow actions beyond displaying the available state and enrichment information.
