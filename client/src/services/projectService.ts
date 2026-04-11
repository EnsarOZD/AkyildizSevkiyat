import apiClient from './apiClient';

export interface Zone {
  id: number;
  name: string;
  order: number;
  description?: string;
}

export interface ProjectCoordinateValidationDto {
  projectId: number;
  projectCode: string;
  projectName: string;
  systemAddress?: string;
  recordedLat?: number;
  recordedLng?: number;
  geocodedLat?: number;
  geocodedLng?: number;
  distanceKm?: number;
  status: 'Compatible' | 'Suspicious' | 'Incompatible' | 'NoAddress' | 'NoCoordinate';
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

  async updateNetsisCariKodu(projectId: number, netsisCariKodu: string | null, netsisTeslimCariKodu?: string | null): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/netsis-cari-kodu`, { netsisCariKodu, netsisTeslimCariKodu });
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
  },

  async updateDeliveryWindow(projectId: number, start: string | null, end: string | null): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/delivery-window`, {
      deliveryWindowStart: start,
      deliveryWindowEnd: end,
    });
  },

  async exportMappings(): Promise<void> {
    const response = await apiClient.get('/projects/export-mappings', {
      responseType: 'blob'
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'ProjeBolgeEslesmeleri.xlsx');
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  },

  async importMappings(file: File): Promise<any> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post('/projects/import-mappings', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  },

  async validateCoordinates(projectIds: number[]): Promise<ProjectCoordinateValidationDto[]> {
    const response = await apiClient.post('/projects/validate-coordinates', { projectIds });
    return response.data;
  },

  async updateLocation(id: number, lat: number | null, lng: number | null): Promise<void> {
    await apiClient.patch(`/projects/${id}/location`, { latitude: lat, longitude: lng });
  },

  async resetLocation(id: number): Promise<void> {
    await apiClient.patch(`/projects/${id}/location`, { latitude: null, longitude: null });
  },
};

export default projectService;
