import apiClient from './apiClient';

export interface ClothingPrepShipment {
  shipmentId: number;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  projectCode: string;
  projectName: string;
  status: string;
  deliveryDate: string;
  lineCount: number;
  preparedByUserName?: string | null;
  koliCount?: string | null;
  netsisTransferred: boolean;
}

export interface ClothingPickLine {
  lineId: number;
  stockCode: string;
  stockName: string;
  orderedQty: number;
  deliveredQty: number;
  unit: string;
  clothingType?: number | null; // 0=Diğer, 1=Ayakkabı
  differenceReason?: string | null;
}

export interface ClothingPickList {
  shipmentId: number;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  projectName: string;
  status: string;
  koliCount?: string | null;
  lines: ClothingPickLine[];
}

export interface ClothingCompleteLineInput {
  shipmentLineId: number;
  deliveredQty: number;
  differenceReason?: string | null;
  note?: string | null;
}

const clothingPrepService = {
  async dashboard(): Promise<ClothingPrepShipment[]> {
    const res = await apiClient.get('/clothing-prep/dashboard');
    return res.data;
  },
  async pickList(shipmentId: number): Promise<ClothingPickList> {
    const res = await apiClient.get(`/clothing-prep/${shipmentId}/pick-list`);
    return res.data;
  },
  async start(shipmentIds: number[]): Promise<{ count: number }> {
    const res = await apiClient.post('/clothing-prep/start', { shipmentIds });
    return res.data;
  },
  async complete(shipmentId: number, koliCount: string | null, lines: ClothingCompleteLineInput[]): Promise<void> {
    await apiClient.post(`/clothing-prep/${shipmentId}/complete`, { koliCount, lines });
  },
};

export default clothingPrepService;
