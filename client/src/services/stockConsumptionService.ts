import apiClient from './apiClient';

export type StockConsumptionType = 0 | 1 | 2; // 0=Zai, 1=DahiliKullanim, 2=DepoSatisi

export const CONSUMPTION_TYPE_LABELS: Record<number, string> = {
  0: 'Zai',
  1: 'Dahili Kullanım',
  2: 'Depo Satışı',
};

export interface StockConsumptionDto {
  id: number;
  stockMasterId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  typeValue: number;
  typeLabel: string;
  quantity: number;
  date: string;
  reason?: string;
  recipientName?: string;
  salePrice?: number;
  totalSaleAmount?: number;
  note?: string;
  createdBy?: string;
  createdAt: string;
}

export interface StockConsumptionListResult {
  items: StockConsumptionDto[];
  totalCount: number;
  pageNumber: number;
  totalPages: number;
}

export interface CreateStockConsumptionPayload {
  stockMasterId: number;
  type: StockConsumptionType;
  quantity: number;
  date: string;
  reason?: string;
  recipientName?: string;
  salePrice?: number;
  note?: string;
}

export interface StockConsumptionFilters {
  fromDate?: string;
  toDate?: string;
  type?: number;
  search?: string;
  page?: number;
  size?: number;
}

const stockConsumptionService = {
  async getAll(filters: StockConsumptionFilters = {}): Promise<StockConsumptionListResult> {
    const params: Record<string, any> = {};
    if (filters.fromDate) params.fromDate = filters.fromDate;
    if (filters.toDate) params.toDate = filters.toDate;
    if (filters.type !== undefined && filters.type !== null) params.type = filters.type;
    if (filters.search) params.search = filters.search;
    if (filters.page) params.page = filters.page;
    if (filters.size) params.size = filters.size;
    const res = await apiClient.get('/stock-consumptions', { params });
    return res.data;
  },

  async create(payload: CreateStockConsumptionPayload): Promise<{ id: number }> {
    const res = await apiClient.post('/stock-consumptions', payload);
    return res.data;
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/stock-consumptions/${id}`);
  },

  getExportUrl(filters: StockConsumptionFilters = {}): string {
    const params = new URLSearchParams();
    if (filters.fromDate) params.set('fromDate', filters.fromDate);
    if (filters.toDate) params.set('toDate', filters.toDate);
    if (filters.type !== undefined && filters.type !== null) params.set('type', String(filters.type));
    if (filters.search) params.set('search', filters.search);
    const base = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '');
    const qs = params.toString();
    return `${base}/stock-consumptions/export${qs ? '?' + qs : ''}`;
  },
};

export default stockConsumptionService;
