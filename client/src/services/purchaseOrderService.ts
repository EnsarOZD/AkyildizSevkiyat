import apiClient from './apiClient';

export interface PurchaseOrder {
  id: string;
  orderNumber: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  supplierId: string;
  supplierNameSnapshot: string;
  status: string;
  lineCount: number;
  emailSentAt?: string;
  emailSentTo?: string;
}

export interface PurchaseOrderQueryParams {
  status?: string;
  supplierName?: string;
  supplierId?: string;
  searchTerm?: string;
  fromDate?: string;
  toDate?: string;
  emailSent?: boolean;
  stockName?: string;
  hidePassive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface PaginatedList<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PurchaseOrderDetail extends PurchaseOrder {
  note?: string;
  lines: PurchaseOrderLine[];
}

export interface PurchaseOrderLine {
  id: string;
  stockMasterId: number;
  stockNameSnapshot: string;
  orderedQty: number;
  unitSnapshot: string;
  note?: string;
}

export interface CreatePORequest {
  supplierId: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  note?: string;
  lines: {
    stockMasterId: number;
    orderedQty: number;
    note?: string;
  }[];
}

const purchaseOrderService = {
  async getAll(params: PurchaseOrderQueryParams): Promise<PaginatedList<PurchaseOrder>> {
    const response = await apiClient.get('/purchase-orders', { params });
    return response.data;
  },

  async getById(id: string | number): Promise<PurchaseOrderDetail> {
    const response = await apiClient.get(`/purchase-orders/${id}`);
    return response.data;
  },

  async getReceivable(params: PurchaseOrderQueryParams): Promise<PurchaseOrder[]> {
    const response = await apiClient.get('/purchase-orders/receivable', { params });
    return response.data || [];
  },

  async create(request: CreatePORequest): Promise<string> {
    const response = await apiClient.post('/purchase-orders', request);
    return response.data; // Assuming it returns the ID as a string
  },

  async approve(id: string): Promise<void> {
    await apiClient.post(`/purchase-orders/${id}/approve`);
  },

  async cancel(id: string): Promise<void> {
    await apiClient.post(`/purchase-orders/${id}/cancel`);
  },

  async close(id: string): Promise<void> {
    await apiClient.post(`/purchase-orders/${id}/close`);
  },

  async markEmailSent(id: string, sentTo?: string): Promise<void> {
    await apiClient.post(`/purchase-orders/${id}/mark-email-sent`, { sentTo: sentTo || null });
  },

  async sendEmail(id: string, pdfBase64?: string): Promise<{ sentTo: string }> {
    const response = await apiClient.post(`/purchase-orders/${id}/send-email`, { pdfBase64: pdfBase64 ?? null });
    return response.data;
  },

  async update(id: string, data: { orderDate?: string; expectedDeliveryDate?: string; note?: string }): Promise<void> {
    await apiClient.put(`/purchase-orders/${id}`, data);
  },

  async addLine(id: string, data: { stockMasterId: number; orderedQty: number; note?: string }): Promise<void> {
    await apiClient.post(`/purchase-orders/${id}/lines`, data);
  },

  async updateLine(id: string, lineId: string, data: { orderedQty?: number; note?: string }): Promise<void> {
    await apiClient.put(`/purchase-orders/${id}/lines/${lineId}`, {
      ...data,
      purchaseOrderId: id,
      lineId: lineId
    });
  },

  async removeLine(id: string, lineId: string): Promise<void> {
    await apiClient.delete(`/purchase-orders/${id}/lines/${lineId}`);
  },

  async getReceivableMaterials(searchTerm?: string): Promise<any[]> {
    const response = await apiClient.get('/purchase-orders/receivable-materials', {
      params: searchTerm ? { searchTerm } : undefined
    });
    return response.data || [];
  },

  async exportToNetsis(id: string): Promise<{ netsisPONo: string; message: string }> {
    const response = await apiClient.post(`/netsis/purchase-orders/${id}/export`);
    return response.data;
  }
};

export default purchaseOrderService;
