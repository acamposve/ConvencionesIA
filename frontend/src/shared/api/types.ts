export interface DocumentSummary {
  summaryText: string;
}

export interface DocumentPersistenceContract {
  id: string;
  tenantId: string;
  source: string;
  format: string;
  fileSizeBytes: number;
  mimeType: string;
  language?: string | null;
  pageCount?: number | null;
  author?: string | null;
  creationDate?: string | null;
  sourceReference: string;
  sourceName?: string | null;
  correlationId: string;
  idempotencyKey: string;
  outcome?: string | null;
  rejectionReason?: string | null;
  processingStage: string;
  state: string;
  detectedDocumentType?: string | null;
  extractedText?: string | null;
  normalizedText?: string | null;
  revisions: Array<{ version: number; timestamp: string; outcome: string; processingStage: string }>;
  documentSummaries?: DocumentSummary[] | null;
  documentClassifications?: Array<{ classificationCode: string; confidenceScore: number }> | null;
}

export interface IngestDocumentResponseContract {
  documentId: string;
  outcome: string;
  processingStage: string;
  rejectionReason?: string | null;
  correlationId: string;
  timestamp: string;
  version?: string;
}
