import { Box, Button, Card, CardContent, Grid, Stack, Typography, Chip, Alert, Divider, Skeleton } from '@mui/material';
import { Link } from 'react-router-dom';
import { useDashboardData } from './useDashboardData';

export default function DashboardPage() {
  const { data, isLoading, isError } = useDashboardData();

  const summaryCards = [
    { title: 'Total documents', value: String(data?.length ?? 0), tone: 'primary' },
    { title: 'In progress', value: String(data?.filter((document) => document.processingStage !== 'Completed' && document.processingStage !== 'Failed').length ?? 0), tone: 'warning' },
    { title: 'Completed', value: String(data?.filter((document) => document.processingStage === 'Completed').length ?? 0), tone: 'success' },
    { title: 'Failed', value: String(data?.filter((document) => document.processingStage === 'Failed').length ?? 0), tone: 'error' }
  ];

  return (
    <Box>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', md: 'center' }} spacing={2} sx={{ mb: 4 }}>
        <Box>
          <Typography variant="h4" fontWeight={700} color="primary.main">Dashboard</Typography>
          <Typography color="text.secondary">Monitor ingestion activity and start a new document workflow.</Typography>
        </Box>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          <Button component={Link} to="/documents/upload" variant="contained">Upload document</Button>
          <Button component={Link} to="/documents/url" variant="outlined">Ingest from URL</Button>
        </Stack>
      </Stack>

      {isLoading ? (
        <>
          <Grid container spacing={2} sx={{ mb: 4 }}>
            {Array.from({ length: 4 }).map((_, index) => (
              <Grid item xs={12} sm={6} md={3} key={index}>
                <Card>
                  <CardContent>
                    <Skeleton variant="text" width="60%" height={20} />
                    <Skeleton variant="text" width="40%" height={36} sx={{ mt: 1 }} />
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
          <Card>
            <CardContent>
              <Skeleton variant="text" width="30%" height={28} sx={{ mb: 2 }} />
              {Array.from({ length: 3 }).map((_, index) => (
                <Box key={index} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2, mb: 1.5 }}>
                  <Skeleton variant="text" width="70%" height={24} />
                  <Skeleton variant="text" width="40%" height={20} sx={{ mt: 1 }} />
                </Box>
              ))}
            </CardContent>
          </Card>
        </>
      ) : (
        <>
          <Grid container spacing={2} sx={{ mb: 4 }}>
            {summaryCards.map((card) => (
              <Grid item xs={12} sm={6} md={3} key={card.title}>
                <Card>
                  <CardContent>
                    <Typography color="text.secondary" variant="body2">{card.title}</Typography>
                    <Typography variant="h4" color={`${card.tone}.main`} fontWeight={700}>{card.value}</Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>Recent documents</Typography>
              {isError ? (
                <Alert severity="warning" action={<Button component={Link} to="/search" size="small">Try search</Button>}>
                  The document list could not be loaded from the API, so demo content is being shown instead.
                </Alert>
              ) : !data?.length ? (
                <Stack spacing={2}>
                  <Alert severity="info">No documents are available yet. Start an ingestion to populate the dashboard.</Alert>
                  <Button component={Link} to="/documents/url" variant="outlined" sx={{ alignSelf: 'flex-start' }}>Try URL ingestion</Button>
                </Stack>
              ) : (
                <Stack spacing={1.5}>
                  {data.map((document) => (
                    <Box key={document.id} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', border: 1, borderColor: 'divider', borderRadius: 2, p: 2 }}>
                      <Box>
                        <Typography fontWeight={600}>{document.sourceReference || document.id}</Typography>
                        <Typography variant="body2" color="text.secondary">{document.source} • {document.id}</Typography>
                      </Box>
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Chip label={document.processingStage} color={document.processingStage === 'Completed' ? 'success' : document.processingStage === 'Failed' ? 'error' : 'warning'} size="small" />
                        <Button component={Link} to={`/documents/${document.id}`} size="small">Open</Button>
                      </Stack>
                    </Box>
                  ))}
                  <Divider />
                  <Button component={Link} to="/documents" variant="text" sx={{ alignSelf: 'flex-start' }}>View all documents</Button>
                </Stack>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </Box>
  );
}
