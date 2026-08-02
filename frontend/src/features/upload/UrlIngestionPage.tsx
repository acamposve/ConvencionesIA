import { Alert, Box, Button, Card, CardContent, Link as MuiLink, Stack, TextField, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useIngestDocument } from './useIngestDocument';

interface UrlIngestionPageProps {
  onNotify?: (message: string, severity?: 'success' | 'info' | 'warning' | 'error') => void;
}

export default function UrlIngestionPage({ onNotify }: UrlIngestionPageProps) {
  const tenantId = 'demo-tenant';
  const correlationId = 'corr-url-1';
  const [sourceReference, setSourceReference] = useState('https://example.com/document.pdf');
  const [validationError, setValidationError] = useState<string | null>(null);
  const { mutate, isPending, isSuccess, isError, data } = useIngestDocument();

  const canSubmit = useMemo(() => Boolean(sourceReference.trim() && /^https?:\/\//i.test(sourceReference.trim())), [sourceReference]);

  const handleSubmit = () => {
    if (!canSubmit) {
      setValidationError('Please enter a valid document URL before submitting.');
      onNotify?.('Please enter a valid document URL before submitting.', 'warning');
      return;
    }

    setValidationError(null);
    onNotify?.('Submitting your URL ingestion request…', 'info');
    mutate({
      tenantId,
      source: 'URL',
      format: 'PDF',
      fileSizeBytes: 1024,
      mimeType: 'application/pdf',
      language: 'en',
      pageCount: 1,
      author: 'demo-user',
      creationDate: new Date().toISOString(),
      sourceReference: sourceReference.trim(),
      correlationId,
      idempotencyKey: `${tenantId}|url|${sourceReference.trim()}`
    }, {
      onSuccess: (response) => {
        onNotify?.(`Submission accepted. Document id: ${response.documentId}`, 'success');
      },
      onError: () => {
        onNotify?.('The document could not be submitted. Check the URL and try again.', 'error');
      }
    });
  };

  return (
    <Box maxWidth={700} sx={{ mx: 'auto' }}>
      <Typography variant="h4" sx={{ mb: 1 }} color="primary.main">Ingest from URL</Typography>
      <Typography color="text.secondary" sx={{ mb: 3 }}>Submit a document URL into the ingestion workflow and move into the shared lifecycle experience.</Typography>
      <Card>
        <CardContent>
          <Stack spacing={2.5}>
            <Typography variant="subtitle2" color="text.secondary">Your tenant and correlation context are inherited from the signed-in session.</Typography>
            <TextField
              label="Document URL"
              value={sourceReference}
              onChange={(event) => setSourceReference(event.target.value)}
              placeholder="https://example.com/document.pdf"
              fullWidth
              inputProps={{ 'aria-label': 'Document URL' }}
              helperText="Use a valid http or https URL for the demo ingestion flow."
            />
            <Button
              type="submit"
              variant="contained"
              onClick={handleSubmit}
              disabled={isPending}
              aria-label={isPending ? 'Submitting URL ingestion request' : 'Submit URL ingestion request'}
            >
              {isPending ? 'Submitting…' : 'Submit'}
            </Button>
            {validationError ? <Alert severity="warning">{validationError}</Alert> : null}
            {isSuccess ? (
              <Alert severity="success">
                Submission accepted. Document id: {data?.documentId}.{' '}
                <MuiLink component={Link} to={`/documents/${data?.documentId}`} underline="hover">Open the document detail view</MuiLink>
              </Alert>
            ) : null}
            {isError ? <Alert severity="error">The document could not be submitted. Check the URL and try again.</Alert> : null}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
