import apiClient from './apiClient';

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
}

export interface MicroPickItemDto {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  totalQty: number;
  pickedQty: number;
  isCompleted: boolean;
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
  DriverId: number;
  VehicleId: number;
}

const warehouseService = {
  async getDashboardAll(params: { date: string }): Promise<DashboardZoneDto[]> {
    const response = await apiClient.get('/warehouse/dashboard-all', { params });
    return response.data;
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

  async setDriverInfo(data: SetZoneDriverInfoRequest): Promise<void> {
    await apiClient.post('/warehouse/set-driver-info', data);
  },

  async fetchIrsaliye(zonePreparationId: number): Promise<{ exported: number; skipped: number; errors: string[] }> {
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
  }
};

export default warehouseService;
