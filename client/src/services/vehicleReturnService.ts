import apiClient from './apiClient';
import type { PaginatedList } from './purchaseOrderService';

export interface VehicleReturnLineDto {
  id: number;
  stockMasterId?: number;
  stockCode: string;
  stockName: string;
  isLinkedToStock: boolean;
  qty: number;
  note?: string;
  status: string;
  linkedShipmentId?: number;
  linkedShipmentIrsaliyeNo?: string;
  resolvedAt?: string;
}

export interface VehicleReturnDto {
  id: number;
  driverSessionId: string;
  driverName: string;
  plateNumber: string;
  returnDate: string;
  note?: string;
  createdAt: string;
  totalLines: number;
  pendingLines: number;
  lines: VehicleReturnLineDto[];
}

export interface VehicleShipmentDto {
  id: number;
  irsaliyeNo?: string;
  orderNumber?: string;
  talepNo?: string;
  projectCode: string;
  projectName: string;
  status: string;
}

export interface CreateVehicleReturnLineRequest {
  stockMasterId?: number;
  stockCodeFree?: string;
  stockNameFree?: string;
  qty: number;
  note?: string;
}

export interface CreateVehicleReturnRequest {
  driverSessionId: string;
  returnDate?: string;
  note?: string;
  lines: CreateVehicleReturnLineRequest[];
}

export const VehicleReturnResolveAction = {
  AddToStock: 1,
  MatchToShipment: 2,
} as const;
export type VehicleReturnResolveAction = typeof VehicleReturnResolveAction[keyof typeof VehicleReturnResolveAction];

export interface ResolveVehicleReturnLineRequest {
  action: VehicleReturnResolveAction;
  linkedShipmentId?: number;
  note?: string;
}

const vehicleReturnService = {
  async getAll(params?: {
    sessionId?: string;
    lineStatus?: number;
    fromDate?: string;
    toDate?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PaginatedList<VehicleReturnDto>> {
    const response = await apiClient.get('/vehiclereturns', { params });
    return response.data;
  },

  async create(request: CreateVehicleReturnRequest): Promise<{ id: number }> {
    const response = await apiClient.post('/vehiclereturns', request);
    return response.data;
  },

  async resolveLine(lineId: number, request: ResolveVehicleReturnLineRequest): Promise<void> {
    await apiClient.post(`/vehiclereturns/lines/${lineId}/resolve`, request);
  },

  async searchShipments(sessionId: string, search: string): Promise<VehicleShipmentDto[]> {
    const response = await apiClient.get('/vehiclereturns/search-shipments', {
      params: { sessionId, search }
    });
    return response.data;
  },
};

export default vehicleReturnService;
