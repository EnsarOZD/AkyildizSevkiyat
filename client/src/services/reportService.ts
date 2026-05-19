import apiClient from './apiClient';

export interface ZoneMaterialReportRow {
  zoneId: number;
  zoneName: string;
  stockCode: string;
  stockName: string;
  unit: string;
  totalQty: number;
  shipmentCount: number;
}

export interface ShipmentSummaryDto {
  total: number;
  created: number;
  assignedToWarehouse: number;
  picking: number;
  readyForDispatch: number;
  assignedToVehicle: number;
  delivered: number;
  cancelled: number;
  passive: number;
  rows: ShipmentSummaryRow[];
}

export interface ShipmentSummaryRow {
  id: number;
  projectName: string;
  zoneName?: string;
  status: string;
  deliveryDate: string;
  driverName?: string;
  talepNo?: string;
  lineCount: number;
}

export interface OpenPurchaseOrderRow {
  id: string;
  orderNumber: string;
  supplierName: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  status: string;
  lineCount: number;
}

export interface PendingGoodsReceiptRow {
  id: string;
  waybillNo: string;
  receiptDate: string;
  supplierName: string;
  linkedOrderNumber?: string;
  lineCount: number;
  status: string;
}

export interface ShipmentPerformanceDto {
  totalDelivered: number;
  onTime: number;
  late: number;
  onTimeRate: number;
  rows: ShipmentPerformanceRow[];
  byZone: ZonePerformanceRow[];
}

export interface ShipmentPerformanceRow {
  id: number;
  projectName: string;
  zoneName?: string;
  talepNo?: string;
  deliveryDate: string;
  deliveredAt: string;
  isLate: boolean;
  delayDays: number;
  driverName?: string;
}

export interface ZonePerformanceRow {
  zoneName: string;
  total: number;
  onTime: number;
  late: number;
  onTimeRate: number;
}

export interface StockStatusReportDto {
  totalStocks: number;
  criticalCount: number;
  outOfStockCount: number;
  rows: StockStatusRow[];
}

export interface StockStatusRow {
  id: number;
  stockCode: string;
  stockName: string;
  category?: string;
  warehouseLocation?: string;
  onHandQty: number;
  reservedQty: number;
  availableQty: number;
  minStockQty?: number;
  isCritical: boolean;
  isOutOfStock: boolean;
}

export interface ReturnsReportDto {
  totalReturnedLines: number;
  totalReturnedQty: number;
  byReason: ReturnReasonSummary[];
  rows: ReturnRow[];
}

export interface ReturnReasonSummary {
  reason: string;
  count: number;
  totalQty: number;
}

export interface ReturnRow {
  shipmentId: number;
  talepNo?: string;
  projectName: string;
  zoneName?: string;
  returnedAt?: string;
  stockCode: string;
  stockName: string;
  returnedQty: number;
  returnReason?: string;
  returnNote?: string;
}

export interface MaterialPurchaseReportRow {
  supplierId: string;
  supplierName: string;
  stockMasterId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  orderedQty: number;
  receivedQty: number;
  remainingQty: number;
}

export interface StockMovementRow {
  id: number;
  date: string;
  stockCode: string;
  stockName: string;
  unit: string;
  transactionType: string;
  transactionTypeId: number;
  qty: number;
  reference?: string;
  note?: string;
}

export interface StockMovementsResult {
  items: StockMovementRow[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface LocationTypeSummary {
  locationTypeId: number;
  locationTypeLabel: string;
  totalOnHand: number;
  totalReserved: number;
  totalAvailable: number;
  locationCount: number;
  stockCount: number;
}

export interface LocationStockRow {
  warehouseLocationId: number;
  locationCode: string;
  locationTypeId: number;
  locationTypeLabel: string;
  alan?: string;
  stockMasterId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  onHandQty: number;
  reservedQty: number;
  availableQty: number;
}

export interface WarehouseStockDistributionDto {
  summary: LocationTypeSummary[];
  details: LocationStockRow[];
}

export const STOCK_TRANSACTION_TYPE_LABELS: Record<number, string> = {
  0: 'Mal Girişi',
  1: 'Sevkiyat Çıkışı',
  2: 'Manuel Düzeltme',
  3: 'Rezervasyon',
  4: 'Rezervasyon İptali',
  5: 'Araç İadesi',
  6: 'Giriş Düzeltmesi',
};

const reportService = {
  async getZoneMaterialReport(params: {
    deliveryDate: string;
    zoneId?: number | null;
    includeDelivered?: boolean;
    qtyMode?: 'Ordered' | 'Delivered';
  }): Promise<ZoneMaterialReportRow[]> {
    const response = await apiClient.get('/reports/zone-material', { params });
    return response.data || [];
  },

  async getShipmentSummary(params: {
    startDate: string;
    endDate: string;
    zoneId?: number | null;
  }): Promise<ShipmentSummaryDto> {
    const response = await apiClient.get('/reports/shipment-summary', { params });
    return response.data;
  },

  async getOpenPurchaseOrders(): Promise<OpenPurchaseOrderRow[]> {
    const response = await apiClient.get('/reports/open-purchase-orders');
    return response.data || [];
  },

  async getPendingGoodsReceipts(): Promise<PendingGoodsReceiptRow[]> {
    const response = await apiClient.get('/reports/pending-goods-receipts');
    return response.data || [];
  },

  async getShipmentPerformance(params: {
    startDate: string;
    endDate: string;
    zoneId?: number | null;
  }): Promise<ShipmentPerformanceDto> {
    const response = await apiClient.get('/reports/shipment-performance', { params });
    return response.data;
  },

  async getStockStatus(params: { criticalOnly?: boolean } = {}): Promise<StockStatusReportDto> {
    const response = await apiClient.get('/reports/stock-status', { params });
    return response.data;
  },

  async getReturns(params: {
    startDate: string;
    endDate: string;
    zoneId?: number | null;
  }): Promise<ReturnsReportDto> {
    const response = await apiClient.get('/reports/returns', { params });
    return response.data;
  },

  async getStockMovements(params: {
    startDate: string;
    endDate: string;
    stockSearch?: string;
    type?: number | null;
    page?: number;
    pageSize?: number;
  }): Promise<StockMovementsResult> {
    const response = await apiClient.get('/reports/stock-movements', { params });
    return response.data;
  },

  async getWarehouseStockDistribution(): Promise<WarehouseStockDistributionDto> {
    const response = await apiClient.get('/reports/warehouse-stock-distribution');
    return response.data;
  },

  async getMaterialPurchases(params?: {
    supplierId?: string | null;
    stockMasterId?: number | null;
    materialName?: string | null;
  }): Promise<MaterialPurchaseReportRow[]> {
    const response = await apiClient.get('/reports/material-purchases', { params });
    return response.data || [];
  },
};

export default reportService;
