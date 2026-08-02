import { useQuery } from '@tanstack/react-query';
import { documentApi } from '@/shared/api/client';

export function useDocumentDetail(documentId?: string) {
  return useQuery({
    queryKey: ['documents', documentId],
    queryFn: () => documentApi.getDocument(documentId!),
    enabled: Boolean(documentId),
    retry: false
  });
}
