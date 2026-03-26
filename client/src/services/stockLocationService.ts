import apiClient from './apiClient';

export interface StockLocationDto {
  id: number;
  stockMasterId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  warehouseLocationId: number;
  locationCode: string;
  zone: string;
  locationType: string;
  onHandQty: number;
  reservedQty: number;
  availableQty: number;
  lastMovedAt?: string;
}

export interface TransferHistoryDto {
  id: number;
  stockCode: string;
  stockName: string;
  fromLocationCode: string;
  toLocationCode: string;
  qty: number;
  note?: string;
  transferredBy?: string;
  transferredAt: string;
}

export interface TransferHistoryResult {
  items: TransferHistoryDto[];
  totalCount: number;
}

const stockLocationService = {
  async getAll(params: {
    stockMasterId?: number;
    warehouseLocationId?: number;
  } = {}): Promise<StockLocationDto[]> {
    const { data } = await apiClient.get('/stock-locations', { params });
    return data;
  },

  async assign(stockMasterId: number, warehouseLocationId: number, qty: number): Promise<void> {
    await apiClient.post('/stock-locations/assign', { stockMasterId, warehouseLocationId, qty });
  },

  async transfer(payload: {
    stockMasterId: number;
    fromLocationId: number;
    toLocationId: number;
    qty: number;
    note?: string;
  }): Promise<void> {
    await apiClient.post('/stock-locations/transfer', payload);
  },

  async getTransferHistory(params: {
    stockMasterId?: number;
    locationId?: number;
    page?: number;
    pageSize?: number;
  } = {}): Promise<TransferHistoryResult> {
    const { data } = await apiClient.get('/stock-locations/transfers', { params });
    return data;
  },
};

export default stockLocationService;
