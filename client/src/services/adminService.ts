import apiClient from './apiClient';

export interface StuckShipmentDto {
  id: number;
  projectName: string;
  talepNo: string | null;
  externalOrderNumber: string | null;
  status: string;
  lineCount: number;
}

export interface ActiveSessionWithShipmentsDto {
  sessionId: string;
  driverId: number;
  driverFullName: string;
  vehicleId: number;
  plateNumber: string;
  startTime: string;
  elapsedMinutes: number;
  shipments: StuckShipmentDto[];
}

export interface MarkDeliveredRequest {
  deliveryRecipient: string;
  overrideNote: string;
  deliveryNote?: string;
}

const adminService = {
  async getActiveOperations(): Promise<ActiveSessionWithShipmentsDto[]> {
    const res = await apiClient.get<ActiveSessionWithShipmentsDto[]>('/admin/active-operations');
    return res.data;
  },

  async forceCloseSession(sessionId: string, notes: string | null): Promise<void> {
    await apiClient.post(`/admin/driver-sessions/${sessionId}/force-close`, { notes });
  },

  async markShipmentDelivered(shipmentId: number, req: MarkDeliveredRequest): Promise<void> {
    await apiClient.post(`/shipments/${shipmentId}/mark-delivered`, req);
  },
};

export default adminService;
