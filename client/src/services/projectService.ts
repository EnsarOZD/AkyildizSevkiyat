import apiClient from './apiClient';

export interface Zone {
  id: number;
  name: string;
  order: number;
  description?: string;
}

const projectService = {
  async getZones(): Promise<Zone[]> {
    const response = await apiClient.get('/zones');
    return response.data || [];
  },

  async getProjects(): Promise<any[]> {
    const response = await apiClient.get('/projects');
    return response.data || [];
  },

  async assignZone(projectId: number, zoneId: number): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/zone`, { zoneId });
  },

  async createZone(data: { name: string; order: number }): Promise<Zone> {
    const response = await apiClient.post<Zone>('/zones', data);
    return response.data;
  },

  async updateZone(id: number, data: { id: number; name: string; order: number }): Promise<void> {
    await apiClient.put(`/zones/${id}`, data);
  },

  async deleteZone(id: number): Promise<void> {
    await apiClient.delete(`/zones/${id}`);
  },

  async updateNetsisCariKodu(projectId: number, netsisCariKodu: string | null): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/netsis-cari-kodu`, { netsisCariKodu });
  },

  async syncProjects(params: { forceAll: boolean } = { forceAll: true }): Promise<{ count: number }> {
    const response = await apiClient.post('/projects/sync', params);
    return response.data;
  },

  async updateDeliveryOrder(projectId: number, deliveryOrder: number | null): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/delivery-order`, { deliveryOrder });
  },

  async bulkUpdateDeliveryOrders(orders: { projectId: number; deliveryOrder: number }[]): Promise<void> {
    await apiClient.patch('/projects/bulk-delivery-orders', { orders });
  }
};

export default projectService;
