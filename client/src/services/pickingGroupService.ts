import apiClient from './apiClient';

export interface PickingGroup {
  id: number;
  name: string;
  sortOrder: number;
  isActive: boolean;
}

const pickingGroupService = {
  getAll: (activeOnly = false) =>
    apiClient.get('/picking-groups', { params: { activeOnly } }).then(r => r.data as PickingGroup[]),
  save: (data: { id?: number | null; name: string; sortOrder: number; isActive: boolean }) =>
    apiClient.post('/picking-groups', data).then(r => r.data as { id: number }),
  deactivate: (id: number) => apiClient.delete(`/picking-groups/${id}`),
};

export default pickingGroupService;
