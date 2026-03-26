import apiClient from './apiClient';

export interface GoodsReceipt {
  id: string;
  waybillNo: string;
  purchaseOrderNumber?: string;
  supplierNameSnapshot: string;
  receiptDate: string;
  status: string;
  lineCount: number;
}

export interface GoodsReceiptLine {
  id: string;
  stockMasterId: number;
  stockNameSnapshot?: string;
  unitSnapshot: string;
  orderedQty: number;
  receivedQty: number;
  acceptedQty?: number;
  rejectedQty?: number;
  rejectReason?: string;
  note?: string;
}

export interface GoodsReceiptDetail extends GoodsReceipt {
  purchaseOrderId?: string;
  supplierId: string;
  waybillDate: string;
  note?: string;
  lines: GoodsReceiptLine[];
}

export interface GoodsReceiptQueryParams {
  pageNumber?: number;
  pageSize?: number;
  status?: string;
  supplierName?: string;
  startDate?: string;
  endDate?: string;
}

const goodsReceiptService = {
  async getAll(params?: GoodsReceiptQueryParams): Promise<any> {
    const response = await apiClient.get('/goods-receipts', { params });
    return response.data;
  },

  async getById(id: string): Promise<GoodsReceiptDetail> {
    const response = await apiClient.get(`/goods-receipts/${id}`);
    return response.data;
  },

  async create(data: {
    purchaseOrderId: string;
    supplierId: string;
    waybillNo: string;
    waybillDate: string;
    receiptDate: string;
    note?: string;
    ignoreDuplicateWarning?: boolean;
  }): Promise<{ id: string; hasDuplicateWarning?: boolean }> {
    const response = await apiClient.post('/goods-receipts', data);
    return response.data;
  },

  async addLine(data: { goodsReceiptId: string; stockMasterId: number; receivedQty: number; note?: string }): Promise<{ id: string }> {
    const response = await apiClient.post(`/goods-receipts/${data.goodsReceiptId}/lines`, data);
    return { id: response.data };
  },

  async updateLine(goodsReceiptId: string, lineId: string, data: {
    receivedQty: number;
    acceptedQty?: number;
    rejectedQty?: number;
    rejectReason?: string;
    note?: string;
  }): Promise<void> {
    await apiClient.put(`/goods-receipts/${goodsReceiptId}/lines/${lineId}`, {
      ...data,
      goodsReceiptId,
      lineId
    });
  },

  async post(id: string): Promise<void> {
    await apiClient.post(`/goods-receipts/${id}/post`);
  },

  async cancel(id: string): Promise<void> {
    await apiClient.post(`/goods-receipts/${id}/cancel`);
  },

  async removeLine(id: string, lineId: string): Promise<void> {
    await apiClient.delete(`/goods-receipts/${id}/lines/${lineId}`);
  },

  async createCorrection(id: string): Promise<{ id: string }> {
    const response = await apiClient.post(`/goods-receipts/${id}/corrections`, { originalGoodsReceiptId: id });
    return { id: response.data };
  }
};

export default goodsReceiptService;
