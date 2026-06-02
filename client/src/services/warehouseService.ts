import apiClient from './apiClient';

/**
 * Eski kayıtlarda backend bazı satırlara otomatik (sistem) fark nedeni yazmış olabilir.
 * Bu değerleri kullanıcıya gösterme — boş kabul et ki ekranda anlamsız teknik metin çıkmasın.
 */
const SYSTEM_REASON_TOKENS = ['BulkEdit', 'Macro Dağıtım', 'Gıda Toplu Toplama', 'Gıda Dağıtım'];
export const cleanDifferenceReason = (r: string | null | undefined): string =>
  r && !SYSTEM_REASON_TOKENS.includes(r.trim()) ? r : '';

export interface PreDispatchSummaryDto {
  zonePreparationId: number;
  totalShipments: number;
  readyShipments: number;
  notReadyShipments: number;
  netsisTransferredCount: number;
  netsisFailedCount: number;
  zeroPickedLineCount: number;
  unmappedLineCount: number;
  diffReasonLineCount: number;
  overPickedLineCount: number;
  hasForceCompleteShipments: boolean;
  forceCompleteShipmentIds: number[];
  openErrorCount: number;
  openWarningCount: number;
  blockers: string[];
  warnings: string[];
  canProceed: boolean;
}

export interface WarehouseStats {
  pendingShipments: number;
  pickingShipments: number;
  readyShipments: number;
  stockShortages: number;
}

export interface DashboardProjectDto {
  id: number;
  projectId: number;
  projectCode: string;
  projectName: string;
  isMicroReady: boolean;
  microReadyAt: string | null;
  isAddedLater: boolean;
  preparedByUserName: string | null;
  pickingLockedByUserName: string | null;
  pickingLockedAt: string | null;
  shipmentId: number | null;
  zonePreparationId: number;
}

export interface VerificationLineDto {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  orderedQty: number;
  deliveredQty: number;
  difference: number;
  differenceReason: string | null;
}

export interface VerificationShipmentDto {
  shipmentId: number;
  talepNo: string | null;
  irsaliyeNo: string | null;
  projectId: number;
  projectName: string;
  projectCode: string;
  lines: VerificationLineDto[];
}

export interface DashboardZoneDto {
  id: number;
  zoneId: number;
  zoneName: string;
  batchNo: number;
  batchLabel: string;
  deliveryDate: string;
  status: string;
  statusId: number;
  isFrozen: boolean;
  irsaliyeFetched: boolean;
  openErrorCount: number;
  openWarningCount: number;
  projects: DashboardProjectDto[];
  isOutOfCity: boolean;
  macroLockedByUserName: string | null;
  macroLockedAt: string | null;
  foodTotalWeightKg: number | null;
  foodPickedWeightKg: number | null;
}

export interface OutOfCitySubLineDto {
  shipmentLineId: number;
  projectId: number;
  projectName: string;
  orderedQty: number;
}

export interface OutOfCityPickItemDto {
  lines: OutOfCitySubLineDto[];
  stockCode: string;
  stockName: string;
  unit: string;
  totalOrderedQty: number;
  totalPickedQty: number;
  isCompleted: boolean;
  category: string | null;
  pickingType: string | null;
  differenceReason: string | null;
}

export interface OutOfCityLineUpdateDto {
  shipmentLineId: number;
  deliveredQty: number;
  differenceReason?: string | null;
  newLocalStockId?: number | null;
}

export interface MarkZoneOutOfCityReadyRequest {
  zonePreparationId: number;
  lines: OutOfCityLineUpdateDto[];
  forceComplete?: boolean;
  forceReason?: string | null;
}

export interface MarkZoneOutOfCityReadyResult {
  success: boolean;
  unfilledLineCount: number;
  warnings: string[];
}

export interface MicroPickItemDto {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  totalQty: number;
  pickedQty: number;
  isCompleted: boolean;
  category: string | null;
  pickingOrder: number;
  differenceReason: string | null;
}

export interface MacroSubLineDto {
  id: number;
  projectId: number;
  projectName: string;
  orderedQty: number;
}

export interface MacroPickItemDto {
  lines: MacroSubLineDto[];
  stockCode: string;
  stockName: string;
  unit: string;
  totalOrderedQty: number;
  totalPickedQty: number;
  projectCount: number;
  isCompleted: boolean;
  category: string | null;
  pickingOrder: number;
  differenceReason: string | null;
}

// ── Gıda Hazırlık ────────────────────────────────────────────────────────────

export interface FoodPickSubLineDto {
  shipmentLineId: number;
  zonePreparationId: number;
  batchNo: number;
  projectId: number;
  projectName: string;
  orderedQty: number;
  pickedQty: number;
}

export interface FoodPickItemDto {
  lines: FoodPickSubLineDto[];
  stockCode: string;
  stockName: string;
  unit: string;
  totalOrderedQty: number;
  totalPickedQty: number;
  projectCount: number;
  batchCount: number;
  isCompleted: boolean;
  pickingOrder: number;
  totalWeightKg: number | null;
  differenceReason: string | null;
}

export interface FoodPickLineUpdateDto {
  shipmentLineIds: number[];
  newTotalPickedQty: number;
  differenceReason?: string | null;
  newLocalStockId?: number | null;
}

export interface UpdateFoodPickLinesRequest {
  updates: FoodPickLineUpdateDto[];
}

export interface MarkFoodPreparationReadyRequest {
  zoneId: number;
  deliveryDate: string;
  forceComplete?: boolean;
  forceReason?: string | null;
}

export interface MarkFoodPreparationReadyResult {
  success: boolean;
  advancedBatchCount: number;
  unfilledFoodLineCount: number;
}

// Request Interfaces
export interface StartZonePreparationRequest {
  zonePreparationId: number;
}

export interface UpdateAggregatedLinesRequest {
  ZonePreparationId: number;
  ShipmentLineIds: number[];
  NewTotalPickedQty: number;
  NewLocalStockId: number | null;
  /** Toplam miktar sipariş miktarından farklıysa zorunlu */
  DifferenceReason?: string;
}

export interface ShortageAllocationDto {
  shipmentLineId: number;
  deliveredQty: number;
}

export interface AllocateMacroShortageRequest {
  zonePreparationId: number;
  allocations: ShortageAllocationDto[];
  differenceReason?: string;
}

export interface MarkZoneMacroReadyRequest {
  ZonePreparationId: number;
  ForceComplete?: boolean;
  ForceReason?: string;
}

export interface MarkZoneMacroReadyResult {
  success: boolean;
  unfilledMacroLineCount: number;
}

export interface MicroLineUpdateDto {
  shipmentLineId: number;
  deliveredQty: number;
  newLocalStockId: number | null;
  /** Toplanan miktar sipariş miktarından farklıysa zorunlu */
  differenceReason?: string;
}

export interface UpdateMicroLinesBulkRequest {
  zonePreparationProjectId: number;
  lines: MicroLineUpdateDto[];
}

export interface MarkProjectMicroReadyRequest {
  ZonePreparationProjectId: number;
  ForceComplete?: boolean;
  ForceReason?: string;
}

export interface MarkProjectMicroReadyResult {
  success: boolean;
  unfilledLineCount: number;
  unmappedLineCount: number;
}

export interface SetZoneDriverInfoRequest {
  ZonePreparationId: number;
  DriverIds: number[];
  VehicleId: number;
  DepartureTime?: string | null;
}

export interface SetZoneDriverInfoResult {
  success: boolean;
  optimizationApplied: boolean;
  optimizationWarning: string | null;
}

export type CargoProviderType = 'MNG' | 'YurticiKargo' | 'Aras' | 'PTT' | 'Other';
export const CARGO_PROVIDERS: { value: number; label: string }[] = [
  { value: 0, label: 'MNG Kargo' },
  { value: 1, label: 'Yurtiçi Kargo' },
  { value: 2, label: 'Aras Kargo' },
  { value: 3, label: 'PTT Kargo' },
  { value: 99, label: 'Diğer' },
];

export interface YkDesiLine {
  count: number;
  desi: number;
}

export interface DispatchAsCargoRequest {
  zonePreparationId: number;
  cargoProvider: number;
  cargoTrackingNumber?: string | null;
  ykDesiLines?: YkDesiLine[] | null;
}

export interface DispatchAsFreightRequest {
  zonePreparationId: number;
  carrierName: string;
  carrierPlate?: string | null;
  carrierPhone?: string | null;
}

export interface YkShipmentStatus {
  cargoKey: string;
  statusCode?: string | null;
  statusDescription?: string | null;
  barcode?: string | null;
  lastUpdate?: string | null;
}

export interface YkCargoReportItem {
  id: number;
  projectCode: string;
  projectName: string;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  shipmentStatus: string;
  deliveryDate: string;
  dispatchedAt?: string | null;
  ykCargoKey?: string | null;
  ykJobId?: number | null;
  ykOperationStatus?: string | null;
  ykOperationMessage?: string | null;
  ykErrorCode?: string | null;
  ykErrorMessage?: string | null;
  ykLastQueryAt?: string | null;
}

const warehouseService = {
  async getDashboardAll(): Promise<DashboardZoneDto[]> {
    const response = await apiClient.get('/warehouse/dashboard-all');
    return response.data;
  },

  async syncDashboard(date: string): Promise<void> {
    await apiClient.post('/warehouse/dashboard/sync', { date });
  },

  async startZonePreparation(payload: StartZonePreparationRequest): Promise<void> {
    await apiClient.post('/warehouse/start-zone-preparation', payload);
  },

  async getMacroPickList(params: { zonePreparationId: number }): Promise<MacroPickItemDto[]> {
    const response = await apiClient.get('/warehouse/macro-pick-list', { params });
    return response.data;
  },

  async updateAggregatedLines(payload: UpdateAggregatedLinesRequest): Promise<void> {
    await apiClient.post('/warehouse/update-aggregated-lines', payload);
  },

  async allocateMacroShortage(payload: AllocateMacroShortageRequest): Promise<void> {
    await apiClient.post('/warehouse/allocate-macro-shortage', payload);
  },

  async markMacroReady(payload: MarkZoneMacroReadyRequest): Promise<MarkZoneMacroReadyResult> {
    const response = await apiClient.post('/warehouse/mark-macro-ready', payload);
    return response.data;
  },

  async getMicroPickList(params: { zpProjectId: number }): Promise<MicroPickItemDto[]> {
    const response = await apiClient.get('/warehouse/micro-pick-list', { params });
    return response.data;
  },

  async updateMicroLinesBulk(payload: UpdateMicroLinesBulkRequest): Promise<void> {
    await apiClient.post('/warehouse/update-micro-lines-bulk', payload);
  },

  async markMicroReady(payload: MarkProjectMicroReadyRequest): Promise<MarkProjectMicroReadyResult> {
    const response = await apiClient.post('/warehouse/mark-micro-ready', payload);
    return response.data;
  },

  async getDashboardStats(): Promise<WarehouseStats> {
    const response = await apiClient.get('/warehouse/stats');
    return response.data;
  },

  async setDriverInfo(data: SetZoneDriverInfoRequest): Promise<SetZoneDriverInfoResult> {
    const response = await apiClient.post('/warehouse/set-driver-info', data);
    return response.data;
  },

  async fetchIrsaliye(zonePreparationId: number): Promise<{ fetched: number; skipped: number; errors: string[]; warnings: string[] }> {
    const response = await apiClient.post('/warehouse/fetch-irsaliye', { zonePreparationId });
    return response.data;
  },

  async getPreDispatchSummary(zpId: number): Promise<PreDispatchSummaryDto> {
    const response = await apiClient.get('/warehouse/pre-dispatch-summary', { params: { zpId } });
    return response.data;
  },

  async confirmLoading(zonePreparationId: number): Promise<boolean> {
    const response = await apiClient.post('/warehouse/confirm-loading', { zonePreparationId });
    return response.data;
  },

  async dispatchAsCargo(data: DispatchAsCargoRequest): Promise<void> {
    await apiClient.post('/warehouse/dispatch-as-cargo', data);
  },

  async dispatchAsFreight(data: DispatchAsFreightRequest): Promise<void> {
    await apiClient.post('/warehouse/dispatch-as-freight', data);
  },

  async getOutOfCityPickList(zonePreparationId: number, projectId?: number | null): Promise<OutOfCityPickItemDto[]> {
    const response = await apiClient.get('/warehouse/out-of-city-pick-list', { params: { zonePreparationId, projectId } });
    return response.data;
  },

  async markOutOfCityReady(data: MarkZoneOutOfCityReadyRequest): Promise<MarkZoneOutOfCityReadyResult> {
    const response = await apiClient.post('/warehouse/mark-out-of-city-ready', data);
    return response.data;
  },

  async saveOutOfCityProgress(data: { zonePreparationId: number; projectId?: number | null; lines: { shipmentLineId: number; deliveredQty: number; newLocalStockId?: number | null; differenceReason?: string | null }[] }): Promise<void> {
    await apiClient.post('/warehouse/save-out-of-city-progress', data);
  },

  async adminForceCloseZone(zonePreparationId: number): Promise<void> {
    await apiClient.post('/warehouse/admin-force-close-zone', { zonePreparationId });
  },

  async lockProjectPicking(zonePreparationProjectId: number, release = false): Promise<void> {
    await apiClient.post('/warehouse/lock-project-picking', { zonePreparationProjectId, release });
  },

  async lockMacroPicking(zonePreparationId: number, release = false): Promise<void> {
    await apiClient.post('/warehouse/lock-zone-macro-picking', { zonePreparationId, release });
  },

  async getZoneVerification(zpId: number): Promise<VerificationShipmentDto[]> {
    const response = await apiClient.get('/warehouse/zone-verification', { params: { zpId } });
    return response.data;
  },

  async getFoodPickList(params: { zoneId: number; deliveryDate: string }): Promise<FoodPickItemDto[]> {
    const response = await apiClient.get('/warehouse/food-pick-list', { params });
    return response.data;
  },

  async updateFoodPickLines(payload: UpdateFoodPickLinesRequest): Promise<boolean> {
    const response = await apiClient.post('/warehouse/update-food-pick-lines', payload);
    return response.data;
  },

  async allocateFoodShortage(payload: {
    allocations: { shipmentLineId: number; deliveredQty: number }[];
    differenceReason: string;
  }): Promise<boolean> {
    const response = await apiClient.post('/warehouse/allocate-food-shortage', payload);
    return response.data;
  },

  async markFoodPreparationReady(payload: MarkFoodPreparationReadyRequest): Promise<MarkFoodPreparationReadyResult> {
    const response = await apiClient.post('/warehouse/mark-food-preparation-ready', payload);
    return response.data;
  },

  async resetGidaHazirlikShipments(zonePreparationId: number): Promise<{ resetCount: number }> {
    const response = await apiClient.post(`/warehouse/zone-preparations/${zonePreparationId}/reset-shipment-status`);
    return response.data;
  },

  async markWarehousePickup(shipmentId: number, recipientName: string, note?: string): Promise<void> {
    await apiClient.post(`/warehouse/shipments/${shipmentId}/warehouse-pickup`, { recipientName, note });
  },

  async queryYkShipmentStatus(shipmentId: number): Promise<YkShipmentStatus | null> {
    const response = await apiClient.get(`/warehouse/shipments/${shipmentId}/yk-status`);
    return response.data;
  },

  async getYkCargoReport(params: {
    search?: string;
    ykStatus?: string;
    startDate?: string;
    endDate?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<{ items: YkCargoReportItem[]; pageIndex: number; totalPages: number; totalCount: number }> {
    const response = await apiClient.get('/shipments/yk-cargo-report', { params });
    const data = response.data;
    return {
      items: data.items ?? [],
      pageIndex: data.pageIndex ?? 1,
      totalPages: data.totalPages ?? 1,
      totalCount: data.totalCount ?? 0,
    };
  }
};

export default warehouseService;
