import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import UploadPage from './UploadPage';

describe('UploadPage', () => {
  it('renders the upload form heading and guidance', () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <UploadPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Upload document')).toBeInTheDocument();
    expect(screen.getByText('Submit a document file into the ingestion workflow and move into the shared lifecycle experience.')).toBeInTheDocument();
  });
});
