import { useMutation, useQueryClient } from '@tanstack/react-query';
import { documentApi } from '@/shared/api/client';

export function useIngestDocument() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: Record<string, unknown>) => documentApi.ingestDocument(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    }
  });
}
