import { describe, expect, it, vi } from 'vitest';
import { documentApi } from './client';
import { getMockDocuments } from './mockData';

describe('documentApi fallback behavior', () => {
  it('returns mock documents when the API request fails', async () => {
    const fallback = getMockDocuments();

    const result = await documentApi.listDocuments('demo-tenant');

    expect(result).toEqual(fallback);
  });
});
