import apiClient from './apiClient';

export interface FreightDeliveryShipmentInfo {
  shipmentId: number;
  irsaliyeNo?: string | null;
  talepNo?: string | null;
  lineCount: number;
}

export interface FreightDeliveryInfo {
  projectName: string;
  carrierName: string;
  isCompleted: boolean;
  isExpired: boolean;
  expiresAt: string;
  recipientName?: string | null;
  shipments: FreightDeliveryShipmentInfo[];
}

const freightDeliveryService = {
  async getInfo(token: string): Promise<FreightDeliveryInfo> {
    const res = await apiClient.get<FreightDeliveryInfo>(`/public/freight-delivery/${token}`);
    return res.data;
  },

  async submitProof(token: string, data: { recipientName: string; note?: string | null; photosBase64: string[] }): Promise<{ deliveredCount: number }> {
    const res = await apiClient.post(`/public/freight-delivery/${token}/proof`, data);
    return res.data;
  },
};

export default freightDeliveryService;
