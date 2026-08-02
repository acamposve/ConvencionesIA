import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import DocumentDetailPage from './DocumentDetailPage';

describe('DocumentDetailPage', () => {
  it('renders the document detail heading', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/documents/doc-001']}>
          <Routes>
            <Route path="/documents/:id" element={<DocumentDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => expect(screen.getByText('Document detail')).toBeInTheDocument());
  });

  it('renders a link back to the documents list', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/documents/doc-001']}>
          <Routes>
            <Route path="/documents/:id" element={<DocumentDetailPage />} />
            <Route path="/documents" element={<div>Documents page</div>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => expect(screen.getByRole('link', { name: /back to documents/i })).toBeInTheDocument());
  });

  it('renders the processing insights sections for summary, classification, clauses, and embeddings', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/documents/doc-001']}>
          <Routes>
            <Route path="/documents/:id" element={<DocumentDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Summary')).toBeInTheDocument();
      expect(screen.getByText('Classification')).toBeInTheDocument();
      expect(screen.getByText('Clauses')).toBeInTheDocument();
      expect(screen.getByText(/Embeddings/i)).toBeInTheDocument();
    });
  });

  it('renders the detail sections and available insight content', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/documents/doc-001']}>
          <Routes>
            <Route path="/documents/:id" element={<DocumentDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText(/Summary/i)).toBeInTheDocument();
      expect(screen.getByText(/Classification/i)).toBeInTheDocument();
      expect(screen.getByText(/A policy document awaiting processing/i)).toBeInTheDocument();
    });
  });
});
