import { Alert, Box, Button, Card, CardContent, Link as MuiLink, Stack, TextField, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useIngestDocument } from './useIngestDocument';

interface UploadPageProps {
  onNotify?: (message: string, severity?: 'success' | 'info' | 'warning' | 'error') => void;
}

export default function UploadPage({ onNotify }: UploadPageProps) {
  const tenantId = 'demo-tenant';
  const correlationId = 'corr-upload-1';
  const [sourceReference, setSourceReference] = useState('https://example.com/upload.pdf');
  const [fileName, setFileName] = useState('sample-document.pdf');
  const [validationError, setValidationError] = useState<string | null>(null);
  const { mutate, isPending, isSuccess, isError, data } = useIngestDocument();

  const canSubmit = useMemo(() => Boolean(sourceReference.trim() && fileName.trim()), [sourceReference, fileName]);

  const handleSubmit = () => {
    if (!canSubmit) {
      setValidationError('Please complete the required fields and choose a file before submitting.');
      onNotify?.('Please complete the required fields and choose a file before submitting.', 'warning');
      return;
    }

    setValidationError(null);
    onNotify?.('Submitting your document to the ingestion workflow…', 'info');
    mutate({
      tenantId: tenantId.trim(),
      source: 'Upload',
      format: 'PDF',
      fileSizeBytes: 2048,
      mimeType: 'application/pdf',
      language: 'en',
      pageCount: 1,
      author: 'demo-user',
      creationDate: new Date().toISOString(),
      sourceReference: sourceReference.trim(),
      correlationId: correlationId.trim(),
      idempotencyKey: `${tenantId.trim()}|upload|${sourceReference.trim()}`
    }, {
      onSuccess: (response) => {
        onNotify?.(`Submission accepted. Document id: ${response.documentId}`, 'success');
      },
      onError: () => {
        onNotify?.('The document could not be submitted. Check the required fields and try again.', 'error');
      }
    });
  };

  return (
    <Box maxWidth={700}>
      <Typography variant="h4" sx={{ mb: 1 }} color="primary.main">Upload document</Typography>
      <Typography color="text.secondary" sx={{ mb: 3 }}>Submit a document file into the ingestion workflow and move into the shared lifecycle experience.</Typography>
      <Card>
        <CardContent>
          <Stack spacing={2.5}>
            <Typography variant="subtitle2" color="text.secondary">Your tenant and correlation context are inherited from the signed-in session.</Typography>
            <TextField label="Source reference" value={sourceReference} onChange={(event) => setSourceReference(event.target.value)} fullWidth inputProps={{ 'aria-label': 'Source reference' }} />
            <TextField label="File name" value={fileName} onChange={(event) => setFileName(event.target.value)} fullWidth inputProps={{ 'aria-label': 'Selected file name' }} helperText="This demo uses the file name as a simple upload label." />
            <Button type="submit" variant="contained" onClick={handleSubmit} disabled={isPending} aria-label="Submit upload request">{isPending ? 'Submitting…' : 'Submit'}</Button>
            {validationError ? <Alert severity="warning">{validationError}</Alert> : null}
            {isSuccess ? (
              <Alert severity="success">
                Submission accepted. Document id: {data?.documentId}.{' '}
                <MuiLink component={Link} to={`/documents/${data?.documentId}`} underline="hover">Open the document detail view</MuiLink>
              </Alert>
            ) : null}
            {isError ? <Alert severity="error">The document could not be submitted. Check the required fields and try again.</Alert> : null}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
