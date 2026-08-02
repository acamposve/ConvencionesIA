import { Alert, Snackbar, Stack } from '@mui/material';
import { useEffect, useState } from 'react';

export interface NotificationItem {
  id: number;
  message: string;
  severity?: 'success' | 'info' | 'warning' | 'error';
}

export function NotificationCenter({ items, onDismiss }: { items: NotificationItem[]; onDismiss: (id: number) => void }) {
  const [visible, setVisible] = useState<NotificationItem | null>(null);

  useEffect(() => {
    if (items.length > 0 && !visible) {
      setVisible(items[0]);
    }
  }, [items, visible]);

  const handleClose = () => {
    if (visible) {
      onDismiss(visible.id);
      setVisible(null);
    }
  };

  return (
    <Snackbar open={Boolean(visible)} autoHideDuration={4000} onClose={handleClose} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
      <Alert severity={visible?.severity ?? 'info'} onClose={handleClose} sx={{ width: '100%' }}>
        {visible?.message}
      </Alert>
    </Snackbar>
  );
}
