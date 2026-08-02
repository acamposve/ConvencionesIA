import { useQuery } from '@tanstack/react-query';
import { documentApi } from '@/shared/api/client';
import type { DocumentPersistenceContract } from '@/shared/api/types';

export function useDashboardData() {
  return useQuery({
    queryKey: ['documents', 'dashboard'],
    queryFn: () => documentApi.listDocuments(),
    select: (documents: DocumentPersistenceContract[]) => documents.slice(0, 5)
  });
}
