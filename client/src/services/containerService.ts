import apiClient from './apiClient';

export interface Container {
  id: number;
  code: string;
  type: number;   // 0=Cart(Araba), 1=Pallet(Palet)
  isActive: boolean;
}

export const ContainerTypeLabels: Record<number, string> = { 0: 'Araba', 1: 'Palet' };

const containerService = {
  getAll: (activeOnly = false) =>
    apiClient.get('/containers', { params: { activeOnly } }).then(r => r.data as Container[]),
  save: (data: { id?: number | null; code: string; type: number; isActive: boolean }) =>
    apiClient.post('/containers', data).then(r => r.data as { id: number }),
  deactivate: (id: number) => apiClient.delete(`/containers/${id}`),
};

export default containerService;
