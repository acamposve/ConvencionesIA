import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import SearchPage from './SearchPage';

vi.mock('@/shared/api/client', async () => {
  const actual = await vi.importActual<typeof import('@/shared/api/client')>('@/shared/api/client');
  return {
    ...actual,
    documentApi: {
      ...actual.documentApi,
      listDocuments: vi.fn().mockResolvedValue([
        {
          id: 'doc-001',
          tenantId: 'demo-tenant',
          source: 'Upload',
          format: 'PDF',
          fileSizeBytes: 1024,
          mimeType: 'application/pdf',
          sourceReference: 'Quarterly policy PDF',
          correlationId: 'corr-001',
          idempotencyKey: 'demo-tenant|upload|policy',
          processingStage: 'Completed',
          state: 'Completed',
          revisions: []
        }
      ])
    }
  };
});

describe('SearchPage', () => {
  it('renders the search input and guidance text', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <SearchPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByLabelText(/search documents/i)).toBeInTheDocument();
    expect(await screen.findAllByText(/search by document reference/i)).toHaveLength(2);
  });

  it('shows a no-results message when the query does not match any document', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <SearchPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    fireEvent.change(screen.getByLabelText(/search documents/i), { target: { value: 'missing' } });

    expect(await screen.findByText(/No documents matched your search/i)).toBeInTheDocument();
  });
});
