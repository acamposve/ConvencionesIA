import { Box, AppBar, Toolbar, Typography, Container, Stack, Button } from '@mui/material';
import { NavLink } from 'react-router-dom';

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static" color="transparent" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, color: 'primary.main', fontWeight: 700 }}>
            Document Ingestion
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button component={NavLink} to="/dashboard" color="inherit">Dashboard</Button>
            <Button component={NavLink} to="/documents" color="inherit">Documents</Button>
            <Button component={NavLink} to="/search" color="inherit">Search</Button>
            <Button component={NavLink} to="/documents/upload" color="inherit">Upload</Button>
            <Button component={NavLink} to="/documents/url" color="inherit">URL</Button>
            <Button component={NavLink} to="/settings" color="inherit">Settings</Button>
          </Stack>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        {children}
      </Container>
    </Box>
  );
}
