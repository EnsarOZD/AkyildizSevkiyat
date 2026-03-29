import apiClient from './apiClient';

export interface Shipment {
  id: number;
  shipmentNumber: string;
  orderNumber: string;
  deliveryDate: string;
  status: string;
  isActive: boolean;
  projectNameSnapshot: string;
  totalQuantity: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ShipmentQueryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: number;
  startDate?: string;
  endDate?: string;
}

export interface IssOrderQueryParams {
  tab?: string;
  page?: number;
  pageSize?: number;
  search?: string;
  zone?: string;
  talepNoStatus?: string;
}

export interface ShipmentDetail extends Shipment {
  history: any[];
  lines: ShipmentLine[];
  driverName?: string;
  plateNumber?: string;
  irsaliyeNo?: string;
  irsaliyeDate?: string;
  netsisTransferredAt?: string;
  deliveredAt?: string;
  deliveryNote?: string;
  deliveryRecipient?: string;
  deliveryPhotoBase64?: string;
  returnedAt?: string;
  returnNote?: string;
  externalOrderNumber?: string;
  talepNo?: string;
  teslimAlacakKisiler?: string;
  teslimAlacakTelefon?: string;
  yoneticiMail?: string;
  aciklama?: string;
  zoneId?: number;
  zoneName?: string;
}

export interface ShipmentLine {
  id: number;
  stockCode: string;
  stockName: string;
  orderedQty: number;
  deliveredQty: number;
  differenceReason?: string;
  note?: string;
  returnedQty?: number;
  returnReason?: string;
}

export interface ReturnLineRequest {
  shipmentLineId: number;
  returnedQty: number;
  returnReason: number; // ReturnReason enum value
}

export interface RecordVehicleReturnRequest {
  lines: ReturnLineRequest[];
  returnNote?: string;
  overrideNote?: string;
}

export interface UpdateShipmentDetailsRequest {
  deliveryDate: string;
  lines: {
    lineId: number;
    stockCode: string;
    stockName: string;
    orderedQty: number;
  }[];
}

export interface UpdateShipmentQuantitiesRequest {
  lines: {
    lineId: number;
    deliveredQty: number;
    differenceReason?: string;
    note?: string;
  }[];
}

export interface VehicleAssignmentRequest {
  driverId: number;
  vehicleId: number;
}

export interface BulkAssignVehicleRequest {
  shipmentIds: number[];
  driverName: string;
  plateNumber: string;
}

export interface BulkAssignVehicleResult {
  successCount: number;
  errors: string[];
  warning?: DriverWarning;
}

export interface DriverWarning {
  activeShipmentCount: number;
  message: string;
}

export interface AssignVehicleResult {
  shipmentId: number;
  warning?: DriverWarning;
}

export interface ZoneItem {
  id: number;
  name: string;
  order: number;
}

export interface RevertToDraftRequest {
  reason: string;
}

export interface BulkShipmentRequest {
  issOrderIds: number[];
}

const shipmentService = {
  async getAll(params: ShipmentQueryParams): Promise<PaginatedResponse<Shipment>> {
    const response = await apiClient.get('/shipments', { params });
    const data = response.data;
    
    // Normalize response casing
    return {
      items: data.items || data.Items || [],
      pageIndex: data.pageIndex ?? data.PageIndex ?? 1,
      totalPages: data.totalPages ?? data.TotalPages ?? 1,
      totalCount: data.totalCount ?? data.TotalCount ?? 0,
      hasPreviousPage: data.hasPreviousPage ?? data.HasPreviousPage ?? false,
      hasNextPage: data.hasNextPage ?? data.HasNextPage ?? false,
    };
  },

  async getDetail(id: number): Promise<ShipmentDetail> {
    const response = await apiClient.get(`/shipments/${id}/detail`);
    return response.data;
  },

  async toggleStatus(id: number, setPassive: boolean, reason?: string): Promise<void> {
    await apiClient.post(`/shipments/${id}/toggle-status?setPassive=${setPassive}`, { reason });
  },

  async assignToWarehouse(id: number): Promise<void> {
    await apiClient.post(`/shipments/${id}/assign-to-warehouse`);
  },

  async startPicking(id: number): Promise<void> {
    await apiClient.post(`/shipments/${id}/start-picking`);
  },

  async markReady(id: number): Promise<void> {
    await apiClient.post(`/shipments/${id}/mark-ready`);
  },

  async revertToDraft(id: number, request: RevertToDraftRequest): Promise<void> {
    await apiClient.post(`/shipments/${id}/revert-to-draft`, request);
  },

  async assignVehicle(id: number, request: VehicleAssignmentRequest): Promise<AssignVehicleResult> {
    const response = await apiClient.post(`/shipments/${id}/assign-vehicle`, request);
    return response.data;
  },

  async updateDetails(id: number, request: UpdateShipmentDetailsRequest): Promise<void> {
    await apiClient.put(`/shipments/${id}/details`, {
      ...request,
      shipmentId: id
    });
  },

  async updateQuantities(id: number, request: UpdateShipmentQuantitiesRequest): Promise<void> {
    await apiClient.put(`/shipments/${id}/quantities`, request);
  },

  async markDelivered(id: number, deliveryNote?: string, deliveryRecipient?: string, deliveryPhotoBase64?: string, overrideNote?: string): Promise<void> {
    await apiClient.post(`/shipments/${id}/mark-delivered`, { deliveryNote, deliveryRecipient, deliveryPhotoBase64, overrideNote });
  },

  async updateIrsaliye(id: number, irsaliyeNo: string, irsaliyeDate: string): Promise<void> {
    await apiClient.put(`/shipments/${id}/irsaliye`, { irsaliyeNo, irsaliyeDate });
  },

  async recordVehicleReturn(id: number, request: RecordVehicleReturnRequest): Promise<void> {
    await apiClient.post(`/shipments/${id}/record-vehicle-return`, request);
  },

  async bulkAssignVehicle(request: BulkAssignVehicleRequest): Promise<BulkAssignVehicleResult> {
    const response = await apiClient.post('/shipments/bulk-assign-vehicle', request);
    return response.data;
  },

  async getZones(): Promise<ZoneItem[]> {
    const response = await apiClient.get('/zones');
    return response.data;
  },

  // ISS Integration Integration
  async getIssOrders(params: IssOrderQueryParams): Promise<PaginatedResponse<any>> {
    const response = await apiClient.get('/issorders', { params });
    const data = response.data;
    return {
      items: data.items || data.Items || [],
      pageIndex: data.pageIndex ?? data.PageIndex ?? 1,
      totalPages: data.totalPages ?? data.TotalPages ?? 1,
      totalCount: data.totalCount ?? data.TotalCount ?? 0,
      hasPreviousPage: data.hasPreviousPage ?? data.HasPreviousPage ?? false,
      hasNextPage: data.hasNextPage ?? data.HasNextPage ?? false,
    };
  },

  async getIssOrderCounts(): Promise<any> {
    const response = await apiClient.get('/issorders/counts');
    const data = response.data;
    // Normalize
    return {
      readyCount: data.readyCount ?? data.ReadyCount ?? 0,
      needsMappingCount: data.needsMappingCount ?? data.NeedsMappingCount ?? 0,
      passiveCount: data.passiveCount ?? data.PassiveCount ?? 0,
    };
  },

  async importOrders(params: { startDate: string; endDate: string }): Promise<{
    totalFromIss: number; newCount: number; skippedCount: number;
    needsMappingCount: number; failedCount: number; batchId: number; errors: string[]
  }> {
    const response = await apiClient.post('/issorders/import', params);
    return response.data;
  },

  async startImportAsync(params: { startDate: string; endDate: string }): Promise<{ batchId: number }> {
    const response = await apiClient.post('/issorders/import-async', params);
    return response.data;
  },

  async getImportBatchStatus(batchId: number): Promise<{
    id: number; status: string; totalFromSource: number; newCount: number;
    skippedCount: number; needsMappingCount: number; failedCount: number;
    durationMs: number; errorSummary: string | null; completedAt: string | null;
  }> {
    const response = await apiClient.get(`/issorders/import-batches/${batchId}`);
    return response.data;
  },

  async getImportBatches(page = 1, pageSize = 20): Promise<{
    items: Array<{
      id: number; requestedStartDate: string; requestedEndDate: string;
      startedAt: string; completedAt: string | null; status: string;
      totalFromSource: number; newCount: number; skippedCount: number;
      needsMappingCount: number; failedCount: number; durationMs: number; errorSummary: string | null;
    }>;
    totalCount: number;
  }> {
    const response = await apiClient.get('/issorders/import-batches', { params: { page, pageSize } });
    return response.data;
  },

  async createShipmentFromIss(issOrderId: number): Promise<void> {
    await apiClient.post('/shipments', { issOrderId });
  },

  async bulkCreateFromIss(request: BulkShipmentRequest): Promise<{ createdCount: number }> {
    const response = await apiClient.post('/shipments/bulk', request);
    return response.data;
  },

  async toggleIssActive(id: number, isActive: boolean): Promise<void> {
    await apiClient.post(`/issorders/${id}/toggle-active?isActive=${isActive}`);
  },

  async checkMappings(): Promise<{ count: number }> {
    const response = await apiClient.post('/issorders/check-mappings');
    return response.data;
  },

  // Stock Mapping logic
  async getUnmappedStocks(): Promise<any[]> {
    const response = await apiClient.get('/stockmappings/unmapped');
    return response.data;
  },

  async createMapping(request: { mappingId: number; localStockId?: number | null; ignore: boolean }): Promise<void> {
    await apiClient.put(`/stockmappings/${request.mappingId}`, request);
  },

  async importStocks(formData: FormData): Promise<{ added: number; updated: number; skipped: number; warnings: string[]; errors: string[] }> {
    const response = await apiClient.post('/stocks/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return response.data;
  },

  async importStockMappings(formData: FormData): Promise<{ mappedCount: number; notFoundMappings: string[]; notFoundStocks: string[]; skipped: string[] }> {
    const response = await apiClient.post('/stockmappings/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return response.data;
  },

  async exportUnmappedStocks(): Promise<Blob> {
    const response = await apiClient.get('/stockmappings/export-unmapped', { responseType: 'blob' });
    return response.data;
  },

  async autoMatchMappings(): Promise<{ matchedCount: number; unmatchedCount: number; ordersUnlocked: number }> {
    const response = await apiClient.post('/stockmappings/auto-match');
    return response.data;
  }
};

export default shipmentService;
