import axios from 'axios';
import { getMockDocument, getMockDocuments, getMockIngestionResponse } from './mockData';

const api = axios.create({
  baseURL: 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
    'x-demo-user': 'demo-user',
    'x-demo-tenant': 'demo-tenant'
  }
});

async function safeRequest<T>(request: () => Promise<T>, fallback: T): Promise<T> {
  try {
    return await request();
  } catch {
    return fallback;
  }
}

export const documentApi = {
  async listDocuments(tenantId = 'demo-tenant') {
    return safeRequest(
      async () => {
        const response = await api.get('/api/v1/documents', { params: { tenantId, page: 1, pageSize: 10 } });
        return response.data;
      },
      getMockDocuments()
    );
  },

  async getDocument(documentId: string) {
    return safeRequest(
      async () => {
        const response = await api.get(`/api/v1/documents/${documentId}`);
        return response.data;
      },
      getMockDocument(documentId) ?? getMockDocuments()[0]
    );
  },

  async ingestDocument(payload: Record<string, unknown>) {
    return safeRequest(
      async () => {
        const response = await api.post('/api/v1/documents/ingestion', payload);
        return response.data;
      },
      getMockIngestionResponse()
    );
  }
};
