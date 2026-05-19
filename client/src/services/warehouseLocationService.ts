import apiClient from './apiClient';

export interface WarehouseLocation {
  id: number;
  code: string;
  koridorNo: number;
  taraf: string;      // "K" | "G"
  modulNo: number;
  kat: number;
  locationType: string;
  locationTypeId: number;
  description?: string;
  maxWeightKg?: number;
  maxPallets?: number;
  isActive: boolean;
  alan?: string;
  qrCode?: string;
  totalFloors?: number;
  innerLevel?: string;
  innerPosition?: number;
  containerTypeId: number; // 0=Pallet, 1=Koli, 2=Kutu
}

export interface MapModuleDto {
  koridorNo: number;
  taraf: string;
  modulNo: number;
  dominantTypeId: number;
  hasActive: boolean;
  allActive: boolean;
  totalLocations: number;
}

export interface GetLocationsResult {
  items: WarehouseLocation[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateLocationRequest {
  koridorNo: number;
  taraf: string;
  modulNo: number;
  kat: number;
  locationType: number;
  description?: string;
  maxWeightKg?: number;
  maxPallets?: number;
}

export interface UpdateLocationRequest {
  locationType: number;
  description?: string;
  maxWeightKg?: number;
  maxPallets?: number;
  isActive: boolean;
  alan?: string;
  qrCode?: string;
  totalFloors?: number;
  containerType?: number;
  innerLevel?: string;
  innerPosition?: number;
}

export interface CreatePickingFaceRequest {
  koridorNo: number;
  taraf: string;
  modulFrom: number;
  modulTo: number;
  containerType: number;    // 0=Palet, 1=Koli, 2=Kutu
  // Palet / Koli — kat range
  katFrom?: number;
  katTo?: number;
  // Kutu only
  innerLevels?: string[];
  positionsPerLevel?: number;
  description?: string;
}

export interface LocationQrResult {
  qrValue: string;
  qrImageBase64: string;
}

export interface BulkCreateRequest {
  koridorNo: number;
  taraf: string;
  modulFrom: number;
  modulTo: number;
  katFrom: number;
  katTo: number;
  locationType: number;
  containerType: number; // 0=Palet, 1=Koli
}

export interface BulkCreateResult {
  created: number;
  skipped: number;
}

export const CONTAINER_TYPE_LABELS: Record<number, string> = {
  0: 'Palet',
  1: 'Koli',
  2: 'Kutu',
};

export const AREA_TYPE_LABELS: Record<number, string> = {
  1: 'Zemin İstif',
  2: 'Mal Kabul',
  7: 'İade',
};

export interface CreateAreaLocationRequest {
  locationType: number;  // 1=FloorStack, 2=Receiving, 7=Returns
  alan: string;
  prefix: string;
  count: number;
  description?: string;
}

const warehouseLocationService = {
  async getAll(params: {
    koridorNo?: number;
    taraf?: string;
    type?: number;
    includeInactive?: boolean;
    page?: number;
    pageSize?: number;
  } = {}): Promise<GetLocationsResult> {
    const { data } = await apiClient.get('/warehouse-locations', { params });
    return data;
  },

  async getMap(): Promise<MapModuleDto[]> {
    const { data } = await apiClient.get<MapModuleDto[]>('/warehouse-locations/map');
    return data;
  },

  async create(req: CreateLocationRequest): Promise<{ id: number }> {
    const { data } = await apiClient.post('/warehouse-locations', req);
    return data;
  },

  async bulkCreate(req: BulkCreateRequest): Promise<BulkCreateResult> {
    const { data } = await apiClient.post('/warehouse-locations/bulk', req);
    return data;
  },

  async update(id: number, req: UpdateLocationRequest): Promise<void> {
    await apiClient.put(`/warehouse-locations/${id}`, req);
  },

  async createPickingFace(req: CreatePickingFaceRequest): Promise<{ created: number; skipped: number }> {
    const { data } = await apiClient.post('/warehouse-locations/picking-face', req);
    return data;
  },

  async getQr(id: number): Promise<LocationQrResult> {
    const { data } = await apiClient.get<LocationQrResult>(`/warehouse-locations/${id}/qr`);
    return data;
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/warehouse-locations/${id}`);
  },

  async bulkDelete(ids: number[]): Promise<{ deleted: number }> {
    const { data } = await apiClient.delete('/warehouse-locations/bulk', { data: ids });
    return data;
  },

  async createAreaLocation(req: CreateAreaLocationRequest): Promise<{ created: number; skipped: number }> {
    const { data } = await apiClient.post('/warehouse-locations/area', req);
    return data;
  },
};

export default warehouseLocationService;
