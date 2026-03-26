import apiClient from './apiClient';

export interface StockCountSummary {
  id: number;
  countDate: string;
  status: string;
  note?: string;
  totalLines: number;
  countedLines: number;
  adjustedLines: number;
  createdAt: string;
  completedAt?: string;
}

export interface StockCountDetail {
  id: number;
  countDate: string;
  status: string;
  note?: string;
  createdAt: string;
  completedAt?: string;
  lines: StockCountLineDetail[];
}

export interface StockCountLineDetail {
  id: number;
  stockMasterId: number;
  stockCode: string;
  stockName: string;
  warehouseLocation?: string;
  expectedQty: number;
  actualQty?: number;
  differenceQty?: number;
  note?: string;
}

export interface CompleteStockCountResult {
  adjustedLines: number;
  totalPositiveDiff: number;
  totalNegativeDiff: number;
}

export interface ImportStockCountResult {
  updatedCount: number;
  skippedCount: number;
  errorCount: number;
}

const stockCountService = {
  async getAll(): Promise<StockCountSummary[]> {
    const res = await apiClient.get('/stockcounts');
    return res.data;
  },

  async getDetail(id: number): Promise<StockCountDetail> {
    const res = await apiClient.get(`/stockcounts/${id}`);
    return res.data;
  },

  async create(countDate: string, note?: string): Promise<{ id: number }> {
    const res = await apiClient.post('/stockcounts', { countDate, note });
    return res.data;
  },

  async updateLines(id: number, lines: { stockCountLineId: number; actualQty: number; note?: string }[]): Promise<void> {
    await apiClient.put(`/stockcounts/${id}/lines`, { lines });
  },

  async complete(id: number): Promise<CompleteStockCountResult> {
    const res = await apiClient.post(`/stockcounts/${id}/complete`);
    return res.data;
  },

  async cancel(id: number): Promise<void> {
    await apiClient.post(`/stockcounts/${id}/cancel`);
  },

  async exportTemplate(id: number): Promise<void> {
    const res = await apiClient.get(`/stockcounts/${id}/export`, { responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `Sayim_Sablon_${id}.xlsx`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  async importExcel(id: number, file: File): Promise<ImportStockCountResult> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post(`/stockcounts/${id}/import`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },
};

export default stockCountService;
