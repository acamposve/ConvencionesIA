import { Alert, Box, Button, Card, CardContent, Stack, TextField, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { documentApi } from '@/shared/api/client';

export default function SearchPage() {
  const [query, setQuery] = useState('');
  const { data = [], isLoading, isError } = useQuery({
    queryKey: ['documents', 'search'],
    queryFn: () => documentApi.listDocuments(),
    retry: false
  });

  const matches = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return [];
    }

    return data.filter((document) => {
      const haystack = [
        document.sourceReference,
        document.id,
        document.correlationId,
        document.tenantId,
        document.source
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();

      return haystack.includes(normalizedQuery);
    });
  }, [data, query]);

  return (
    <Box>
      <Stack spacing={2} sx={{ mb: 3 }}>
        <Typography variant="h4" color="primary.main">Search</Typography>
        <Typography color="text.secondary">Search by document reference, ID, correlation ID, tenant, or source.</Typography>
      </Stack>

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField
              label="Search documents"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Try 'policy' or 'corr-001'"
            />

            {isLoading ? <Alert severity="info">Searching documents...</Alert> : null}
            {isError ? <Alert severity="warning">The search service is unavailable. Please try again later.</Alert> : null}
            {!query ? <Alert severity="info">Search by document reference, ID, correlation ID, tenant, or source.</Alert> : null}

            {!isLoading && !isError && query && matches.length === 0 ? (
              <Alert severity="info">No documents matched your search.</Alert>
            ) : null}

            {matches.length > 0 ? (
              <Stack spacing={1.5}>
                {matches.map((document) => (
                  <Box key={document.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2 }}>
                    <Typography fontWeight={600}>{document.sourceReference || document.id}</Typography>
                    <Typography variant="body2" color="text.secondary">{document.source} • {document.id}</Typography>
                    <Button component={Link} to={`/documents/${document.id}`} size="small" sx={{ mt: 1 }}>
                      Open document
                    </Button>
                  </Box>
                ))}
              </Stack>
            ) : null}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
