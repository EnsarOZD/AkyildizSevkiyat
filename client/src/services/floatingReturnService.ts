import apiClient from './apiClient';
import type { PaginatedList } from './purchaseOrderService';

export interface FloatingReturnDto {
  id: number;
  returnDate: string;
  stockCode: string;
  stockName: string;
  isLinkedToStock: boolean;
  qty: number;
  returnReason: string;
  status: string;
  note?: string;
  linkedShipmentId?: number;
  createdAt: string;
  resolvedAt?: string;
}

export interface CreateFloatingReturnRequest {
  returnDate: string;
  stockMasterId?: number;
  stockCodeFree?: string;
  stockNameFree?: string;
  qty: number;
  returnReason: number; // ReturnReason enum value
  note?: string;
}

export const ResolveAction = {
  MatchToShipment: 1,
  AddToStock: 2,
  WriteOff: 3,
} as const;
export type ResolveAction = typeof ResolveAction[keyof typeof ResolveAction];

export interface ResolveFloatingReturnRequest {
  action: ResolveAction;
  linkedShipmentId?: number;
  note?: string;
  /** Serbest (stok kartına bağlı olmayan) iadeyi stoğa eklerken seçilen kart. */
  stockMasterId?: number;
}

const floatingReturnService = {
  async getAll(params?: {
    status?: number;
    fromDate?: string;
    toDate?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PaginatedList<FloatingReturnDto>> {
    const response = await apiClient.get('/floatingreturns', { params });
    return response.data;
  },

  async create(request: CreateFloatingReturnRequest): Promise<{ id: number }> {
    const response = await apiClient.post('/floatingreturns', request);
    return response.data;
  },

  async resolve(id: number, request: ResolveFloatingReturnRequest): Promise<void> {
    await apiClient.post(`/floatingreturns/${id}/resolve`, request);
  },
};

export default floatingReturnService;
