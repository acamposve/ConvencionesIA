import type { DocumentPersistenceContract, IngestDocumentResponseContract } from './types';

const mockDocuments: DocumentPersistenceContract[] = [
  {
    id: 'doc-001',
    tenantId: 'demo-tenant',
    source: 'Upload',
    format: 'PDF',
    fileSizeBytes: 2048,
    mimeType: 'application/pdf',
    language: 'en',
    pageCount: 2,
    author: 'demo-user',
    creationDate: '2026-08-01T10:00:00Z',
    sourceReference: 'Quarterly policy PDF',
    sourceName: 'Quarterly policy PDF',
    correlationId: 'corr-upload-001',
    idempotencyKey: 'demo-tenant|upload|quarterly-policy',
    outcome: 'Accepted',
    rejectionReason: null,
    processingStage: 'PendingProcessing',
    state: 'Accepted',
    detectedDocumentType: 'Policy',
    extractedText: 'Quarterly policy document ready for review.',
    normalizedText: 'Quarterly policy document ready for review.',
    revisions: [],
    documentSummaries: [{ summaryText: 'A policy document awaiting processing.' }],
    documentClassifications: [{ classificationCode: 'POLICY', confidenceScore: 0.93 }]
  },
  {
    id: 'doc-002',
    tenantId: 'demo-tenant',
    source: 'URL',
    format: 'PDF',
    fileSizeBytes: 1024,
    mimeType: 'application/pdf',
    language: 'en',
    pageCount: 1,
    author: 'demo-user',
    creationDate: '2026-08-01T09:30:00Z',
    sourceReference: 'Contract from website',
    sourceName: 'Contract from website',
    correlationId: 'corr-url-001',
    idempotencyKey: 'demo-tenant|url|contract',
    outcome: 'Completed',
    rejectionReason: null,
    processingStage: 'Completed',
    state: 'Completed',
    detectedDocumentType: 'Contract',
    extractedText: 'Contract document parsed successfully.',
    normalizedText: 'Contract document parsed successfully.',
    revisions: [],
    documentSummaries: [{ summaryText: 'A completed contract document with extracted clauses.' }],
    documentClassifications: [{ classificationCode: 'CONTRACT', confidenceScore: 0.95 }]
  }
];

export function getMockDocuments(): DocumentPersistenceContract[] {
  return mockDocuments.map((document) => ({ ...document }));
}

export function getMockDocument(documentId: string): DocumentPersistenceContract | undefined {
  return getMockDocuments().find((document) => document.id === documentId);
}

export function getMockIngestionResponse(): IngestDocumentResponseContract {
  return {
    documentId: 'doc-003',
    outcome: 'Accepted',
    processingStage: 'PendingProcessing',
    correlationId: 'corr-demo-001',
    timestamp: new Date().toISOString(),
    version: 'v1'
  };
}
