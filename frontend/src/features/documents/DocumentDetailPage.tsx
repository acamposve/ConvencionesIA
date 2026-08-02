import { Alert, Box, Button, Card, CardContent, Chip, Divider, Grid, Skeleton, Stack, Typography } from '@mui/material';
import { Link, useParams } from 'react-router-dom';
import { useDocumentDetail } from './useDocumentDetail';

export default function DocumentDetailPage() {
  const { id } = useParams();
  const { data, isLoading, isError } = useDocumentDetail(id);

  if (isLoading) {
    return (
      <Box>
        <Skeleton variant="text" width="40%" height={40} sx={{ mb: 2 }} />
        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Skeleton variant="text" width="70%" height={28} />
              <Skeleton variant="text" width="50%" height={22} />
              <Skeleton variant="rectangular" height={120} />
              <Skeleton variant="text" width="60%" height={24} />
              <Skeleton variant="text" width="100%" height={20} />
            </Stack>
          </CardContent>
        </Card>
      </Box>
    );
  }

  if (isError || !data) {
    return <Alert severity="error">The selected document could not be loaded.</Alert>;
  }

  return (
    <Box>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} spacing={2} sx={{ mb: 3 }}>
        <Typography variant="h4" color="primary.main">Document detail</Typography>
        <Button component={Link} to="/documents" variant="outlined" size="small">
          Back to documents
        </Button>
      </Stack>
      <Card>
        <CardContent>
          <Stack spacing={3}>
            <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
              <Box>
                <Typography variant="h6">{data.sourceReference || data.id}</Typography>
                <Typography color="text.secondary">{data.source} • {data.id}</Typography>
              </Box>
              <Stack direction="row" spacing={1} flexWrap="wrap">
                <Chip label={data.processingStage} color={data.processingStage === 'Completed' ? 'success' : data.processingStage === 'Failed' ? 'error' : 'warning'} />
                <Chip label={data.outcome ?? 'Pending'} color="primary" />
              </Stack>
            </Stack>

            <Divider />

            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" color="text.secondary">Metadata</Typography>
                <Typography color="text.secondary">Tenant: {data.tenantId}</Typography>
                <Typography color="text.secondary">Correlation id: {data.correlationId}</Typography>
                <Typography color="text.secondary">Format: {data.format}</Typography>
                <Typography color="text.secondary">Mime type: {data.mimeType}</Typography>
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" color="text.secondary">Processing insight</Typography>
                <Typography color="text.secondary">State: {data.state}</Typography>
                <Typography color="text.secondary">Detected type: {data.detectedDocumentType ?? 'Not yet available'}</Typography>
                <Typography color="text.secondary">Language: {data.language ?? 'Unknown'}</Typography>
              </Grid>
            </Grid>

            <Box>
              <Typography variant="subtitle2" color="text.secondary">Summary</Typography>
              {data.documentSummaries?.length ? (
                <Typography color="text.secondary" sx={{ mt: 1 }}>{data.documentSummaries[0].summaryText}</Typography>
              ) : (
                <Alert severity="info" sx={{ mt: 1 }}>No summary is available yet for this document. The ingestion pipeline will populate it after processing.</Alert>
              )}
            </Box>

            <Box>
              <Typography variant="subtitle2" color="text.secondary">Classification</Typography>
              {data.documentClassifications?.length ? (
                <Typography color="text.secondary" sx={{ mt: 1 }}>{data.documentClassifications[0].classificationCode} ({data.documentClassifications[0].confidenceScore})</Typography>
              ) : (
                <Alert severity="info" sx={{ mt: 1 }}>No classification is available yet for this document. The classifier will emit a code once the document is processed.</Alert>
              )}
            </Box>

            <Box>
              <Typography variant="subtitle2" color="text.secondary">Clauses</Typography>
              {data.normalizedText ? (
                <Typography color="text.secondary" sx={{ mt: 1 }}>Clause details are ready for review once the pipeline exposes the normalized text.</Typography>
              ) : (
                <Alert severity="info" sx={{ mt: 1 }}>No clause details are available yet for this document.</Alert>
              )}
            </Box>

            <Box>
              <Typography variant="subtitle2" color="text.secondary">Embeddings</Typography>
              {data.detectedDocumentType ? (
                <Typography color="text.secondary" sx={{ mt: 1 }}>Embedding data is prepared for downstream search once processing completes.</Typography>
              ) : (
                <Alert severity="info" sx={{ mt: 1 }}>Embeddings are not available yet for this document.</Alert>
              )}
            </Box>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
