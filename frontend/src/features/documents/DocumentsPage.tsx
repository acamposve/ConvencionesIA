import { Alert, Box, Button, Card, CardContent, Chip, Divider, List, ListItem, ListItemButton, ListItemText, Skeleton, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { documentApi } from '@/shared/api/client';

export default function DocumentsPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['documents', 'list'],
    queryFn: () => documentApi.listDocuments()
  });

  return (
    <Box>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} spacing={2} sx={{ mb: 3 }}>
        <Box>
          <Typography variant="h4" color="primary.main">Documents</Typography>
          <Typography color="text.secondary">Review the documents that have entered the ingestion workflow.</Typography>
        </Box>
        <Button component={Link} to="/documents/upload" variant="contained">New upload</Button>
      </Stack>

      <Card>
        <CardContent>
          <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 2 }}>
            Review the current document inventory and jump into the detail experience for anything that needs attention.
          </Typography>
          {isLoading ? (
            <Stack spacing={1.5} sx={{ py: 1 }}>
              {Array.from({ length: 3 }).map((_, index) => (
                <Box key={index} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2 }}>
                  <Skeleton variant="text" width="60%" height={24} />
                  <Skeleton variant="text" width="40%" height={20} sx={{ mt: 1 }} />
                </Box>
              ))}
            </Stack>
          ) : null}
          {isError ? (
            <Alert severity="warning" action={<Button component={Link} to="/search" size="small">Try search</Button>}>
              The document list could not be loaded; showing demo data instead.
            </Alert>
          ) : null}
          {!isLoading && !isError && (!data || data.length === 0) ? <Alert severity="info">No documents yet. Start with an upload or URL ingestion.</Alert> : null}
          {!isLoading && !isError && data && data.length > 0 ? (
            <List disablePadding>
              {data.map((document, index) => (
                <Box key={document.id}>
                  <ListItem disablePadding>
                    <ListItemButton component={Link} to={`/documents/${document.id}`} aria-label={`Open document ${document.sourceReference || document.id}`} sx={{ borderRadius: 2, px: 1.5, py: 1 }}>
                      <ListItemText
                        primary={document.sourceReference || document.id}
                        secondary={`${document.source} • ${document.id}`}
                      />
                      <Stack direction="row" spacing={1} alignItems="center" sx={{ ml: 2 }}>
                        <Chip label={document.processingStage} color={document.processingStage === 'Completed' ? 'success' : document.processingStage === 'Failed' ? 'error' : 'warning'} size="small" />
                        <Typography variant="body2" color="text.secondary">Open</Typography>
                      </Stack>
                    </ListItemButton>
                  </ListItem>
                  {index < data.length - 1 ? <Divider component="li" /> : null}
                </Box>
              ))}
            </List>
          ) : null}
        </CardContent>
      </Card>
    </Box>
  );
}
