import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import SettingsPage from './SettingsPage';

describe('SettingsPage', () => {
  it('renders the settings heading and controls', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Settings')).toBeInTheDocument();
    expect(screen.getByLabelText(/enable demo mode/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/show guidance hints/i)).toBeInTheDocument();
  });

  it('shows a success message after saving preferences', async () => {
    render(<SettingsPage />);

    fireEvent.click(screen.getByRole('button', { name: /save preferences/i }));

    await waitFor(() => {
      expect(screen.getByText(/Preferences saved/i)).toBeInTheDocument();
    });
  });
});
