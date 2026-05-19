import apiClient from './apiClient';

export interface StockMappingDto {
  id: number;
  externalCode: string;
  externalName: string;
  status: 'Unmapped' | 'Mapped' | 'Ignored';
  statusValue: number;
  localStockId: number | null;
  localStockCode: string | null;
  localStockName: string | null;
  netsisStockCode: string | null;
}

export interface StockMappingPage {
  items: StockMappingDto[];
  totalCount: number;
  totalPages: number;
  pageIndex: number;
}

export interface MapStockRequest {
  mappingId: number;
  localStockId?: number | null;
  ignore: boolean;
}

export interface AutoMatchResult {
  matchedCount: number;
  unmatchedCount: number;
  ordersUnlocked: number;
}

export interface ImportMappingsResult {
  mappedCount: number;
  notFoundMappings: string[];
  notFoundStocks: string[];
  skipped: string[];
}

const stockMappingService = {
  async getAll(params: {
    statusFilter?: string;
    search?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<StockMappingPage> {
    const response = await apiClient.get('/stockmappings', { params });
    return response.data;
  },

  async getUnmapped(): Promise<StockMappingDto[]> {
    const response = await apiClient.get('/stockmappings/unmapped');
    return response.data;
  },

  async map(request: MapStockRequest): Promise<void> {
    await apiClient.put(`/stockmappings/${request.mappingId}`, request);
  },

  async autoMatch(): Promise<AutoMatchResult> {
    const response = await apiClient.post('/stockmappings/auto-match');
    return response.data;
  },

  async importMappings(formData: FormData): Promise<ImportMappingsResult> {
    const response = await apiClient.post('/stockmappings/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  async exportUnmapped(): Promise<Blob> {
    const response = await apiClient.get('/stockmappings/export-unmapped', {
      responseType: 'blob',
    });
    return response.data;
  },
};

export default stockMappingService;
