import apiClient from './apiClient';

export interface Stock {
    id: number;
    stockCode: string;
    stockName: string;
    unit?: string;
    unitId?: number;
    unitPrice?: number;
    taxRate?: number;
    pickingType?: string;
    pickingTypeId?: number;
    category?: string;
    categoryId?: number;
    brand?: string;
    minStockQty?: number;
    reorderPoint?: number | null;
    warehouseLocation?: string;
    onHandQty?: number;
    reservedQty?: number;
    netsisStockCode?: string | null;
    isActive?: boolean;
    weightKg?: number | null;
    pickingOrder?: number;
    barcode?: string | null;
}

export interface PaginatedResponse<T> {
    items: T[];
    pageIndex: number;
    totalPages: number;
    totalCount: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

export interface StockQueryParams {
    search?: string | null;
    page?: number;
    size?: number;
    categoryId?: number | null;
    pickingTypeId?: number | null;
    unitId?: number | null;
    isActive?: boolean | null;
    excludeCategoryId?: number | null;
}

export interface StockCreateRequest {
    stockCode: string;
    stockName: string;
    unit?: number;
    unitPrice?: number;
    taxRate?: number;
    pickingType: number;
    category?: number;
    brand?: string;
    minStockQty?: number | null;
    warehouseLocation?: string;
    netsisStockCode?: string | null;
    weightKg?: number | null;
    pickingOrder?: number;
    barcode?: string | null;
}

export interface StockUpdateRequest extends Partial<StockCreateRequest> {
    id: number;
    barcode?: string | null;
}

export interface ImportStocksResult {
    added: number;
    updated: number;
    skipped: number;
    total: number;
    warnings: string[];
    errors: string[];
}

export const stockService = {
    /**
     * Get paginated stocks with optional filtering and normalized response
     */
    async getAll(params: StockQueryParams): Promise<PaginatedResponse<Stock>> {
        const response = await apiClient.get<any>('/stocks', { params });
        const data = response.data;

        // Normalize backend casing differences (items/Items, pageIndex/PageIndex, etc.)
        return {
            items: data.items || data.Items || [],
            pageIndex: data.pageIndex ?? data.PageIndex ?? 1,
            totalPages: data.totalPages ?? data.TotalPages ?? 1,
            totalCount: data.totalCount ?? data.TotalCount ?? 0,
            hasPreviousPage: data.hasPreviousPage ?? data.HasPreviousPage ?? false,
            hasNextPage: data.hasNextPage ?? data.HasNextPage ?? false
        };
    },

    /**
     * Create a new stock
     */
    async create(data: StockCreateRequest): Promise<Stock> {
        const response = await apiClient.post<Stock>('/stocks', data);
        return response.data;
    },

    /**
     * Update existing stock
     */
    async update(id: number, data: StockUpdateRequest): Promise<void> {
        await apiClient.put(`/stocks/${id}`, data);
    },

    /**
     * Update only the Netsis stock code (partial patch)
     */
    async updateNetsisCode(id: number, netsisStockCode: string | null): Promise<void> {
        await apiClient.patch(`/stocks/${id}/netsis-code`, { netsisStockCode });
    },

    /**
     * Delete a stock
     */
    async delete(id: number): Promise<void> {
        await apiClient.delete(`/stocks/${id}`);
    },

    /**
     * Export stocks to excel
     */
    async export(): Promise<Blob> {
        const response = await apiClient.get('/stocks/export', { responseType: 'blob' });
        return response.data;
    },

    /**
     * Import stocks from excel
     */
    async import(file: File): Promise<ImportStocksResult> {
        const formData = new FormData();
        formData.append('file', file);
        const response = await apiClient.post<ImportStocksResult>('/stocks/import', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return response.data;
    },

    /**
     * Download stock import template
     */
    async downloadTemplate(): Promise<Blob> {
        const response = await apiClient.get('/stocks/template', { responseType: 'blob' });
        return response.data;
    },

    /**
     * Update min stock qty and reorder point thresholds for a stock
     */
    async updateThresholds(id: number, data: { minStockQty?: number | null; reorderPoint?: number | null }): Promise<void> {
        await apiClient.put(`/stocks/${id}/thresholds`, {
            ...data,
            stockMasterId: id
        });
    },

    /**
     * Tek ürün için manuel stok sayımı/girişi.
     * mode 0 = Count (girilen = yeni mevcut), 1 = Add (mevcuda ekle).
     * Yeni OnHandQty değerini döner.
     */
    async adjustOnHand(id: number, data: { quantity: number; mode: 0 | 1; note?: string | null }): Promise<number> {
        const response = await apiClient.post<number>(`/stocks/${id}/adjust-onhand`, {
            stockMasterId: id,
            quantity: data.quantity,
            mode: data.mode,
            note: data.note ?? null
        });
        return response.data;
    }
};
