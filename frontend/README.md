# Frontend

This folder contains the Vite + React + TypeScript user interface for the document ingestion workflow.

## What the frontend covers

The current UI provides a lightweight experience for:

- reviewing a dashboard of ingestion activity
- browsing the document inventory
- opening document details
- starting upload or URL-based ingestion flows

## Stack

- React 19 with TypeScript
- Vite for local development and builds
- Material UI for layout and components
- React Router for navigation
- TanStack Query for data fetching and cache state
- Axios for API calls
- React Hook Form and Zod for form handling and validation

## Project structure

- src/App.tsx: route definitions for the main application shell
- src/features/: feature-oriented pages such as dashboard, documents, upload, and layout
- src/shared/api/client.ts: API client and fallback behavior for local development
- src/test/: shared test setup and utilities

## Frontend specifications

The feature specs for the UI live in [.specify/frontend/specs](../.specify/frontend/specs). The current set includes:

- dashboard
- document-list
- document-details
- document-processing
- document-summary
- document-classification
- clause-explorer
- embeddings
- search
- settings
- error-handling
- notifications
- file-upload
- url-ingestion

## Development commands

From the frontend folder:

- npm install
- npm run dev
- npm run build
- npm test

## Runtime notes

- The client targets the backend at http://localhost:5000.
- Requests include demo tenant and user headers for local exploration.
- If the API is unavailable, the client falls back to mock data so the UI can still be exercised locally.

## Current status

The frontend is a functional scaffold for the document ingestion experience. It is designed to evolve alongside the backend contract and should be updated whenever the ingestion workflow, API responses, or validation rules change.
