import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import UrlIngestionPage from './UrlIngestionPage';
import { useIngestDocument } from './useIngestDocument';

vi.mock('./useIngestDocument', () => ({
  useIngestDocument: vi.fn()
}));

const mockedUseIngestDocument = vi.mocked(useIngestDocument);

function renderPage() {
  const queryClient = new QueryClient();

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <UrlIngestionPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('UrlIngestionPage', () => {
  beforeEach(() => {
    mockedUseIngestDocument.mockReset();
  });

  it('renders the URL ingestion form heading and guidance', () => {
    mockedUseIngestDocument.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: false,
      isError: false,
      data: undefined
    } as never);

    renderPage();

    expect(screen.getByText('Ingest from URL')).toBeInTheDocument();
    expect(screen.getByText(/your tenant and correlation context are inherited from the signed-in session/i)).toBeInTheDocument();
  });

  it('shows validation feedback when the URL is missing', () => {
    mockedUseIngestDocument.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: false,
      isError: false,
      data: undefined
    } as never);

    renderPage();
    fireEvent.change(screen.getByLabelText(/document url/i), { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: /submit url ingestion request/i }));

    expect(screen.getByText(/please enter a valid document url/i)).toBeInTheDocument();
  });

  it('shows the loading state while the submission is in flight', () => {
    mockedUseIngestDocument.mockReturnValue({
      mutate: vi.fn(),
      isPending: true,
      isSuccess: false,
      isError: false,
      data: undefined
    } as never);

    renderPage();

    expect(screen.getByRole('button', { name: /submitting/i })).toBeDisabled();
  });

  it('shows success feedback and a detail link after the submission succeeds', () => {
    mockedUseIngestDocument.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: true,
      isError: false,
      data: { documentId: 'doc-123' }
    } as never);

    renderPage();

    expect(screen.getByText(/submission accepted/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /open the document detail view/i })).toHaveAttribute('href', '/documents/doc-123');
  });

  it('shows an error alert when the submission fails', () => {
    mockedUseIngestDocument.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: false,
      isError: true,
      data: undefined
    } as never);

    renderPage();

    expect(screen.getByText(/the document could not be submitted/i)).toBeInTheDocument();
  });
});
