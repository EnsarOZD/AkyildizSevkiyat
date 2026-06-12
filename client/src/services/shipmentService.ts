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
  talepTuru?: string;
  institutionCode?: string;
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
  operationType?: string; // "Catering" | "Clothing"
}

export interface DeliveryPhoto {
  id: number;
  photoUrl: string;
  photoIndex: number;
  takenAt: string;
}

export interface ShipmentPrintLog {
  id: number;
  printedAt: string;
  printedByName: string;
}

export interface ShipmentStockMovement {
  date: string;
  type: string;
  qty: number;
  stockCode?: string | null;
  stockName?: string | null;
  note?: string | null;
}

export interface ShipmentDetail extends Shipment {
  history: any[];
  lines: ShipmentLine[];
  printLogs: ShipmentPrintLog[];
  stockMovements?: ShipmentStockMovement[];
  projectCode?: string;
  projectName?: string;
  projectAddress?: string;
  driverName?: string;
  plateNumber?: string;
  irsaliyeNo?: string;
  irsaliyeDate?: string;
  netsisTransferredAt?: string;
  deliveredAt?: string;
  deliveryNote?: string;
  deliveryRecipient?: string;
  deliveryPhotoBase64?: string;
  deliveryPhotoPath?: string;
  deliveryPhotos?: DeliveryPhoto[];
  returnedAt?: string;
  returnNote?: string;
  externalOrderNumber?: string;
  talepNo?: string;
  talepTuru?: string;
  institutionCode?: string;
  teslimAlacakKisiler?: string;
  teslimAlacakTelefon?: string;
  yoneticiMail?: string;
  aciklama?: string;
  zoneId?: number;
  zoneName?: string;
  zonePreparationId?: number;
  ykCargoKey?: string | null;
  ykInvoiceKey?: string | null;
  ykJobId?: number | null;
  ykBarcode?: string | null;
  ykOperationStatus?: string | null;
  ykOperationMessage?: string | null;
  ykErrorCode?: string | null;
  ykErrorMessage?: string | null;
  ykLastQueryAt?: string | null;
}

export interface ShipmentLine {
  id: number;
  stockCode: string;
  localStockCode?: string;
  stockName: string;
  orderedQty: number;
  deliveredQty: number;
  unit?: string;
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
  // Termin tarihi değiştiyse: 0=None, 1=Erteleme(Postpone), 2=Diğer(Other)
  dateChangeReason?: number;
  // Erteleme mailine CC eklenecek seçili harici adresler
  extraCc?: string[];
}

export interface UpdateShipmentDetailsResult {
  dateChanged: boolean;
  emailSent: boolean;
  emailError?: string | null;
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
  driverId: number;
  vehicleId: number;
}

export interface BulkAssignVehicleResult {
  successCount: number;
  errors: string[];
}

export interface BulkDispatchResult {
  successCount: number;
  errors: string[];
}

export interface BulkMarkDeliveredResult {
  successCount: number;
  errors: string[];
}

export interface AssignVehicleResult {
  shipmentId: number;
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

export interface DeliverStopLineInput {
  shipmentLineId: number;
  deliveredQty: number;
  returnReason?: number | null;
  returnReasonText?: string | null;
}

export interface DeliverStopExternalReturnInput {
  stockMasterId?: number | null;
  stockCodeFree?: string | null;
  stockNameFree?: string | null;
  qty: number;
  returnReason: number;
  note?: string | null;
}

export interface DeliverStopRequest {
  deliveryRecipient: string;
  deliveryNote?: string | null;
  photosBase64: string[];
  latitude?: number | null;
  longitude?: number | null;
  lines: DeliverStopLineInput[];
  externalReturns?: DeliverStopExternalReturnInput[];
}

export interface DeliverStopResult {
  deliveredShipments: number;
  returnedShipments: number;
  floatingReturns: number;
}

export interface BulkShipmentFailure {
  issOrderId: number;
  reason: string;
}

export interface BulkShipmentResult {
  successCount: number;
  failureCount: number;
  failures: BulkShipmentFailure[];
}

export interface CreateManualShipmentLineInput {
  stockMasterId: number;
  qty: number;
}

export interface CreateManualShipmentRequest {
  customerId: number;
  deliveryDate: string;
  requiresWarehousePreparation: boolean;
  lines: CreateManualShipmentLineInput[];
  notes?: string | null;
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

  async createManual(request: CreateManualShipmentRequest): Promise<number> {
    const response = await apiClient.post('/shipments/manual', request);
    return response.data?.id ?? response.data?.Id;
  },

  async getDetail(id: number): Promise<ShipmentDetail> {
    const response = await apiClient.get(`/shipments/${id}/detail`);
    return response.data;
  },

  async toggleStatus(id: number, setPassive: boolean, reason?: string): Promise<void> {
    await apiClient.post(`/shipments/${id}/toggle-status?setPassive=${setPassive}`, { reason });
  },

  async cancelShipment(id: number, reason: string, notifyOutOfStock: boolean, extraCc?: string[]): Promise<{ emailSent: boolean; emailError?: string | null }> {
    const res = await apiClient.post(`/shipments/${id}/cancel`, { reason, notifyOutOfStock, extraCc });
    const d = res.data ?? {};
    return {
      emailSent: d.emailSent ?? d.EmailSent ?? false,
      emailError: d.emailError ?? d.EmailError ?? null,
    };
  },

  async assignToWarehouse(id: number): Promise<{ warnings?: string[] } | null> {
    const response = await apiClient.post(`/shipments/${id}/assign-to-warehouse`);
    return response.status === 200 ? response.data : null;
  },

  async startPicking(id: number): Promise<void> {
    await apiClient.post(`/shipments/${id}/start-picking`);
  },

  async markReady(id: number): Promise<{ warnings?: string[] } | null> {
    const response = await apiClient.post(`/shipments/${id}/mark-ready`);
    return response.status === 200 ? response.data : null;
  },

  async revertToDraft(id: number, request: RevertToDraftRequest): Promise<void> {
    await apiClient.post(`/shipments/${id}/revert-to-draft`, request);
  },

  async deleteDraft(id: number): Promise<void> {
    await apiClient.delete(`/shipments/${id}/draft`);
  },

  async adminReset(id: number, reason: string): Promise<void> {
    await apiClient.post(`/shipments/${id}/admin-reset`, { reason });
  },

  async revertDelivered(id: number, reason: string): Promise<void> {
    await apiClient.post(`/shipments/${id}/revert-delivered`, { reason });
  },

  async logMissingMail(id: number): Promise<void> {
    await apiClient.post(`/shipments/${id}/log-missing-mail`);
  },

  async sendComparisonEmail(id: number, ccEmails?: string[]): Promise<{ sentTo: string }> {
    const response = await apiClient.post(`/shipments/${id}/send-comparison-email`, { ccEmails: ccEmails ?? [] });
    return response.data;
  },

  async assignVehicle(id: number, request: VehicleAssignmentRequest): Promise<AssignVehicleResult> {
    const response = await apiClient.post(`/shipments/${id}/assign-vehicle`, request);
    return response.data;
  },

  async updateDetails(id: number, request: UpdateShipmentDetailsRequest): Promise<UpdateShipmentDetailsResult> {
    const res = await apiClient.put(`/shipments/${id}/details`, {
      ...request,
      shipmentId: id
    });
    const d = res.data ?? {};
    return {
      dateChanged: d.dateChanged ?? d.DateChanged ?? false,
      emailSent: d.emailSent ?? d.EmailSent ?? false,
      emailError: d.emailError ?? d.EmailError ?? null,
    };
  },

  async updateQuantities(id: number, request: UpdateShipmentQuantitiesRequest): Promise<void> {
    await apiClient.put(`/shipments/${id}/quantities`, request);
  },

  async markDelivered(id: number, deliveryNote?: string, deliveryRecipient?: string, deliveryPhotosBase64?: string[], overrideNote?: string, deliveryLatitude?: number, deliveryLongitude?: number): Promise<void> {
    await apiClient.post(`/shipments/${id}/mark-delivered`, { deliveryNote, deliveryRecipient, deliveryPhotosBase64, overrideNote, deliveryLatitude, deliveryLongitude });
  },

  async updateIrsaliye(id: number, irsaliyeNo: string, irsaliyeDate: string): Promise<void> {
    await apiClient.put(`/shipments/${id}/irsaliye`, { irsaliyeNo, irsaliyeDate });
  },

  async recordVehicleReturn(id: number, request: RecordVehicleReturnRequest): Promise<void> {
    await apiClient.post(`/shipments/${id}/record-vehicle-return`, request);
  },

  async addNote(id: number, note: string): Promise<void> {
    await apiClient.post(`/shipments/${id}/note`, { note });
  },

  async bulkAssignVehicle(request: BulkAssignVehicleRequest): Promise<BulkAssignVehicleResult> {
    const response = await apiClient.post('/shipments/bulk-assign-vehicle', request);
    return response.data;
  },

  async bulkMarkDelivered(shipmentIds: number[], deliveryRecipient: string, overrideNote: string): Promise<BulkMarkDeliveredResult> {
    const response = await apiClient.post('/shipments/bulk-mark-delivered', { shipmentIds, deliveryRecipient, overrideNote });
    return response.data;
  },

  async bulkDispatchAsCargo(shipmentIds: number[], cargoProvider: number, cargoTrackingNumber?: string): Promise<BulkDispatchResult> {
    const response = await apiClient.post('/shipments/bulk-dispatch-cargo', { shipmentIds, cargoProvider, cargoTrackingNumber });
    return response.data;
  },

  async bulkDispatchAsFreight(shipmentIds: number[], carrierName: string, carrierPlate?: string, carrierPhone?: string): Promise<BulkDispatchResult> {
    const response = await apiClient.post('/shipments/bulk-dispatch-freight', { shipmentIds, carrierName, carrierPlate, carrierPhone });
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

  async deliverStop(projectId: number, payload: DeliverStopRequest): Promise<DeliverStopResult> {
    const res = await apiClient.post(`/shipments/stops/${projectId}/deliver`, payload);
    const d = res.data ?? {};
    return {
      deliveredShipments: d.deliveredShipments ?? d.DeliveredShipments ?? 0,
      returnedShipments: d.returnedShipments ?? d.ReturnedShipments ?? 0,
      floatingReturns: d.floatingReturns ?? d.FloatingReturns ?? 0,
    };
  },

  async bulkCreateFromIss(request: BulkShipmentRequest): Promise<BulkShipmentResult> {
    const response = await apiClient.post('/shipments/bulk', request);
    const d = response.data ?? {};
    return {
      successCount: d.successCount ?? d.SuccessCount ?? 0,
      failureCount: d.failureCount ?? d.FailureCount ?? 0,
      failures: (d.failures ?? d.Failures ?? []).map((f: any) => ({
        issOrderId: f.issOrderId ?? f.IssOrderId,
        reason: f.reason ?? f.Reason ?? 'Bilinmeyen hata',
      })),
    };
  },

  async toggleIssActive(id: number, isActive: boolean): Promise<void> {
    await apiClient.post(`/issorders/${id}/toggle-active?isActive=${isActive}`);
  },

  async checkMappings(): Promise<{ count: number }> {
    const response = await apiClient.post('/issorders/check-mappings');
    return response.data;
  },

  async checkNetsisTransfers(options?: { fromDate?: string; toDate?: string; checkTransferred?: boolean }): Promise<{ checked: number; markedAsTransferred: number; resetToActive: number; netsisDeletedCount: number; totalPending: number; error?: string }> {
    const body = {
      fromDate: options?.fromDate ? new Date(options.fromDate).toISOString() : undefined,
      toDate: options?.toDate ? new Date(options.toDate).toISOString() : undefined,
      checkTransferred: options?.checkTransferred ?? false,
    };
    const response = await apiClient.post('/issorders/check-netsis-transfers', body, { timeout: 0 });
    return response.data;
  },

  async checkSingleOrderNetsis(orderNumber: string): Promise<{ found: boolean; wasTransferred: boolean; existsInNetsis: boolean; reverted: boolean; message: string; externalOrderNumber?: string }> {
    const response = await apiClient.post('/issorders/check-single-order-netsis', { orderNumber }, { timeout: 0 });
    return response.data;
  },

  async bulkDeactivateOrders(ids: number[]): Promise<{ count: number }> {
    const response = await apiClient.post('/issorders/bulk-deactivate', { ids });
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
  },

  async exportToNetsis(id: number): Promise<{ netsisOrderNo: string; message: string }> {
    const response = await apiClient.post(`/netsis/shipments/${id}/export`);
    return response.data;
  },

  async bulkExportToNetsis(shipmentIds: number[]): Promise<{ exported: number; skipped: number; errors: string[] }> {
    const response = await apiClient.post('/netsis/shipments/bulk-export', { shipmentIds });
    return response.data;
  },

  async exportClothingToNetsis(id: number): Promise<{ netsisOrderNo: string; irsaliyeNo?: string; warnings: string[]; message: string }> {
    const response = await apiClient.post(`/netsis/shipments/${id}/export-clothing`);
    return response.data;
  },

  async fetchShipmentIrsaliye(id: number): Promise<{ irsaliyeNo?: string; message: string }> {
    const response = await apiClient.post(`/netsis/shipments/${id}/fetch-irsaliye`);
    return response.data;
  },

  async verifyNetsisTransfers(filters?: any): Promise<{ message: string }> {
    const response = await apiClient.post('/netsis/shipments/verify-transfers', filters || {});
    return response.data;
  },

  async recoverNetsisTransfers(): Promise<{ checked: number; recovered: number; notFound: number; error?: string }> {
    const response = await apiClient.post('/netsis/shipments/recover-transfers', {}, { timeout: 0 });
    return response.data;
  },

  async logPrint(id: number): Promise<{
    logId: number;
    printedAt: string;
    printedByName: string;
    wasPreviouslyPrinted: boolean;
    previousPrintCount: number;
  }> {
    const response = await apiClient.post(`/shipments/${id}/log-print`);
    return response.data;
  },
};

export default shipmentService;
