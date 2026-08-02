import { Alert, Box, Button, Card, CardContent, FormControlLabel, Stack, Switch, Typography } from '@mui/material';
import { useState } from 'react';

export default function SettingsPage() {
  const [demoMode, setDemoMode] = useState(true);
  const [showHints, setShowHints] = useState(true);
  const [saved, setSaved] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSave = () => {
    if (!demoMode && !showHints) {
      setErrorMessage('Select at least one preference before saving.');
      return;
    }

    setErrorMessage(null);
    setSaved(true);
  };

  return (
    <Box>
      <Stack spacing={2} sx={{ mb: 3 }}>
        <Typography variant="h4" color="primary.main">Settings</Typography>
        <Typography color="text.secondary">Adjust lightweight demo preferences for the document ingestion experience.</Typography>
      </Stack>

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Alert severity="info">These settings are demo-oriented and do not affect the backend contract.</Alert>
            <FormControlLabel
              control={<Switch checked={demoMode} onChange={() => setDemoMode((value) => !value)} />}
              label="Enable demo mode"
            />
            <FormControlLabel
              control={<Switch checked={showHints} onChange={() => setShowHints((value) => !value)} />}
              label="Show guidance hints"
            />
            {saved ? <Alert severity="success">Preferences saved for this demo session.</Alert> : null}
            {errorMessage ? <Alert severity="warning">{errorMessage}</Alert> : null}
            <Button variant="contained" sx={{ alignSelf: 'flex-start' }} onClick={handleSave}>Save preferences</Button>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
