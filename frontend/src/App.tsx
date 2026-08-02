import { Routes, Route, Navigate } from 'react-router-dom';
import { useState } from 'react';
import { Layout } from './features/layout/Layout';
import DashboardPage from './features/dashboard/DashboardPage';
import DocumentsPage from './features/documents/DocumentsPage';
import UploadPage from './features/upload/UploadPage';
import UrlIngestionPage from './features/upload/UrlIngestionPage';
import DocumentDetailPage from './features/documents/DocumentDetailPage';
import SearchPage from './features/search/SearchPage';
import SettingsPage from './features/settings/SettingsPage';
import { NotificationCenter, type NotificationItem } from './shared/components/notifications';

export default function App() {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);

  const pushNotification = (message: string, severity: NotificationItem['severity'] = 'info') => {
    setNotifications((current) => [...current, { id: Date.now(), message, severity }]);
  };

  const dismissNotification = (id: number) => {
    setNotifications((current) => current.filter((item) => item.id !== id));
  };

  return (
    <>
      <Layout>
        <Routes>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/documents" element={<DocumentsPage />} />
        <Route path="/documents/upload" element={<UploadPage onNotify={pushNotification} />} />
        <Route path="/documents/url" element={<UrlIngestionPage onNotify={pushNotification} />} />
        <Route path="/documents/:id" element={<DocumentDetailPage />} />
        <Route path="/search" element={<SearchPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Routes>
    </Layout>
    <NotificationCenter items={notifications} onDismiss={dismissNotification} />
    </>
  );
}
