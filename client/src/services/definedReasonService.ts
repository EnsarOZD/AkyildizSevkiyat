import apiClient from './apiClient';

export interface DefinedReason {
  id: number;
  category: number;
  label: string;
  sortOrder: number;
  isActive: boolean;
}

export interface DefinedReasonInput {
  category: number;
  label: string;
  sortOrder: number;
}

export const definedReasonService = {
  getAll: (category?: number) =>
    apiClient.get('/defined-reasons', { params: { category } }).then(r => r.data as DefinedReason[]),

  create: (data: DefinedReasonInput) =>
    apiClient.post('/defined-reasons', data).then(r => r.data),

  update: (id: number, data: DefinedReasonInput & { id: number; isActive: boolean }) =>
    apiClient.put(`/defined-reasons/${id}`, data),

  remove: (id: number) =>
    apiClient.delete(`/defined-reasons/${id}`),
};
