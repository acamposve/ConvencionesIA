import { render, screen } from '@testing-library/react';
import { NotificationCenter } from './NotificationCenter';

describe('NotificationCenter', () => {
  it('renders a visible notification when items are provided', () => {
    render(<NotificationCenter items={[{ id: 1, message: 'Upload complete', severity: 'success' }]} onDismiss={() => undefined} />);

    expect(screen.getByText('Upload complete')).toBeInTheDocument();
  });
});
