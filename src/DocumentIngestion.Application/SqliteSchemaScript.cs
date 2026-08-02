namespace DocumentIngestion.Application;

public static class SqliteSchemaScript
{
    public const string CreateTablesSql = @"
        CREATE TABLE IF NOT EXISTS tenants (
            tenant_id TEXT PRIMARY KEY,
            tenant_code TEXT NOT NULL UNIQUE,
            created_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS documents (
            document_id TEXT PRIMARY KEY,
            tenant_id TEXT NOT NULL,
            document_key TEXT NOT NULL UNIQUE,
            source TEXT NOT NULL,
            source_reference TEXT NOT NULL,
            source_name TEXT,
            document_type TEXT NOT NULL,
            format TEXT NOT NULL,
            ingestion_state TEXT NOT NULL,
            current_outcome TEXT NOT NULL,
            current_processing_stage TEXT NOT NULL,
            correlation_id TEXT,
            idempotency_key TEXT,
            file_size_bytes INTEGER NOT NULL,
            mime_type TEXT NOT NULL,
            language TEXT,
            page_count INTEGER,
            author TEXT,
            creation_date TEXT,
            raw_text TEXT,
            normalized_text TEXT,
            rejection_reason TEXT,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            FOREIGN KEY (tenant_id) REFERENCES tenants(tenant_id)
        );

        CREATE TABLE IF NOT EXISTS document_revisions (
            revision_id TEXT PRIMARY KEY,
            document_id TEXT NOT NULL,
            version INTEGER NOT NULL,
            revision_timestamp TEXT NOT NULL,
            outcome TEXT NOT NULL,
            processing_stage TEXT NOT NULL,
            created_at TEXT NOT NULL,
            FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE,
            UNIQUE(document_id, version)
        );

        CREATE TABLE IF NOT EXISTS clauses (
            clause_id TEXT PRIMARY KEY,
            document_id TEXT NOT NULL,
            sequence INTEGER NOT NULL,
            number_label TEXT,
            text TEXT NOT NULL,
            span_start INTEGER NOT NULL,
            span_end INTEGER NOT NULL,
            created_at TEXT NOT NULL,
            FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS clause_category_assignments (
            clause_id TEXT NOT NULL,
            category_code TEXT NOT NULL,
            confidence_score REAL NOT NULL,
            created_at TEXT NOT NULL,
            PRIMARY KEY (clause_id, category_code, created_at),
            FOREIGN KEY (clause_id) REFERENCES clauses(clause_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS document_classifications (
            document_id TEXT NOT NULL,
            classification_code TEXT NOT NULL,
            confidence_score REAL NOT NULL,
            created_at TEXT NOT NULL,
            PRIMARY KEY (document_id, created_at),
            FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS document_summaries (
            summary_id TEXT PRIMARY KEY,
            document_id TEXT NOT NULL,
            summary_text TEXT NOT NULL,
            created_at TEXT NOT NULL,
            FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS document_embeddings (
            embedding_id TEXT PRIMARY KEY,
            document_id TEXT NOT NULL,
            embedding_vector TEXT NOT NULL,
            status TEXT NOT NULL,
            created_at TEXT NOT NULL,
            FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS processing_events (
            event_id TEXT PRIMARY KEY,
            tenant_id TEXT NOT NULL,
            document_id TEXT,
            event_type TEXT NOT NULL,
            event_message TEXT,
            created_at TEXT NOT NULL,
            FOREIGN KEY (tenant_id) REFERENCES tenants(tenant_id),
            FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS idx_documents_tenant_created ON documents(tenant_id, created_at);
        CREATE UNIQUE INDEX IF NOT EXISTS idx_documents_tenant_idempotency ON documents(tenant_id, idempotency_key);
    ";
}
