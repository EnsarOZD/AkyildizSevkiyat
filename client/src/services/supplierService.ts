import apiClient from './apiClient';

export interface Supplier {
    id: string;
    name: string;
    supplierCode?: string;
    email?: string;
}

export interface SupplierCreateParams {
    name: string;
    supplierCode?: string;
    email?: string;
}

export interface SupplierUpdateParams {
    name: string;
    supplierCode?: string;
    email?: string;
}

export const supplierService = {
    /**
     * Get all suppliers with optional search filtering
     */
    async getAll(search?: string): Promise<Supplier[]> {
        const response = await apiClient.get<Supplier[]>('/suppliers', {
            params: { search: search || null }
        });
        return response.data;
    },

    /**
     * Create a new supplier
     */
    async create(data: SupplierCreateParams): Promise<Supplier> {
        const response = await apiClient.post<Supplier>('/suppliers', data);
        return response.data;
    },

    /**
     * Update an existing supplier
     */
    async update(id: string, data: SupplierUpdateParams): Promise<void> {
        await apiClient.put(`/suppliers/${id}`, data);
    },

    /**
     * Import suppliers from Excel
     */
    async import(formData: FormData): Promise<{ count: number }> {
        const response = await apiClient.post<{ count: number }>('/suppliers/import', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return response.data;
    },

    /**
     * Download supplier import template
     */
    async downloadTemplate(): Promise<Blob> {
        const response = await apiClient.get('/suppliers/template', { responseType: 'blob' });
        return response.data;
    }
};
