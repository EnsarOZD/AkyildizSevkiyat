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
}

export interface BulkCreateRequest {
  koridorNo: number;
  taraf: string;
  modulFrom: number;
  modulTo: number;
  katFrom: number;
  katTo: number;
  locationType: number;
}

export interface BulkCreateResult {
  created: number;
  skipped: number;
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
};

export default warehouseLocationService;
