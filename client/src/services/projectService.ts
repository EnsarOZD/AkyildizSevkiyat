import apiClient from './apiClient';

export interface Zone {
  id: number;
  name: string;
  order: number;
  isOutOfCity: boolean;
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
  cityName?: string | null;
  districtName?: string | null;
  status: 'Compatible' | 'Suspicious' | 'Incompatible' | 'NoAddress' | 'NoCoordinate';
}

export interface ProjectQueryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  showInactive?: boolean;
}

export interface PaginatedProjects {
  items: any[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

const projectService = {
  async getZones(): Promise<Zone[]> {
    const response = await apiClient.get('/zones');
    return response.data || [];
  },

  async getProjects(params: ProjectQueryParams = {}): Promise<PaginatedProjects> {
    const response = await apiClient.get('/projects', { params });
    const d = response.data;
    return {
      items: d.items || d.Items || [],
      pageIndex: d.pageIndex ?? d.PageIndex ?? 1,
      totalPages: d.totalPages ?? d.TotalPages ?? 1,
      totalCount: d.totalCount ?? d.TotalCount ?? 0,
      hasPreviousPage: d.hasPreviousPage ?? d.HasPreviousPage ?? false,
      hasNextPage: d.hasNextPage ?? d.HasNextPage ?? false,
    };
  },

  async toggleProjectActive(projectId: number, isActive: boolean): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/toggle-active`, null, { params: { isActive } });
  },

  async deleteProject(projectId: number): Promise<void> {
    await apiClient.delete(`/projects/${projectId}`);
  },

  async assignZone(projectId: number, zoneId: number): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/zone`, { zoneId });
  },

  async createZone(data: { name: string; order: number; isOutOfCity: boolean }): Promise<Zone> {
    const response = await apiClient.post<Zone>('/zones', data);
    return response.data;
  },

  async updateZone(id: number, data: { id: number; name: string; order: number; isOutOfCity: boolean }): Promise<void> {
    await apiClient.put(`/zones/${id}`, data);
  },

  async deleteZone(id: number): Promise<void> {
    await apiClient.delete(`/zones/${id}`);
  },

  async updateNetsisCariKodu(projectId: number, netsisCariKodu: string | null, netsisTeslimCariKodu?: string | null): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/netsis-cari-kodu`, { netsisCariKodu, netsisTeslimCariKodu });
  },

  async syncProjects(params: { forceAll: boolean } = { forceAll: false }, config?: { timeout?: number }): Promise<{ count: number }> {
    const response = await apiClient.post('/projects/sync', params, config);
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

  async updateLocation(id: number, lat: number | null, lng: number | null, cityName?: string | null, districtName?: string | null): Promise<void> {
    await apiClient.patch(`/projects/${id}/location`, { latitude: lat, longitude: lng, cityName, districtName });
  },

  async resetLocation(id: number): Promise<void> {
    await apiClient.patch(`/projects/${id}/location`, { latitude: null, longitude: null });
  },

  async getContacts(): Promise<any[]> {
    const response = await apiClient.get('/projects/contacts');
    return response.data;
  },

  async updateContact(projectId: number, contactName: string | null, contactPhone: string | null): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/contact`, { contactName, contactPhone });
  },
};

export default projectService;
