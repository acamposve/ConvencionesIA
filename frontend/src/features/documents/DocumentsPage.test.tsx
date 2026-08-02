import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import DocumentsPage from './DocumentsPage';
import { documentApi } from '@/shared/api/client';

vi.mock('@/shared/api/client', async () => {
  const actual = await vi.importActual<typeof import('@/shared/api/client')>('@/shared/api/client');
  return {
    ...actual,
    documentApi: {
      ...actual.documentApi,
      listDocuments: vi.fn()
    }
  };
});

describe('DocumentsPage', () => {
  it('renders the documents list from the API response', async () => {
    vi.mocked(documentApi.listDocuments).mockResolvedValue([
      {
        id: 'doc-1',
        tenantId: 'demo-tenant',
        correlationId: 'corr-1',
        source: 'Upload',
        format: 'PDF',
        mimeType: 'application/pdf',
        language: 'en',
        pageCount: 1,
        author: 'demo-user',
        creationDate: '2026-01-01T00:00:00Z',
        sourceReference: 'Quarterly report',
        processingStage: 'Completed',
        state: 'Completed',
        outcome: 'Success',
        detectedDocumentType: 'Report',
        documentSummaries: [],
        documentClassifications: []
      }
    ] as never);

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DocumentsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText('Quarterly report')).toBeInTheDocument();
    expect(screen.getByText('Upload • doc-1')).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
  });
});
