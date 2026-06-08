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

  async aggregatePickList(shipmentIds: number[]): Promise<ClothingAggregatePickList> {
    const res = await apiClient.post('/clothing-prep/aggregate/pick-list', { shipmentIds });
    return res.data;
  },
  async distributeAggregate(shipmentIds: number[], lines: AggPickInput[]): Promise<void> {
    await apiClient.post('/clothing-prep/aggregate/distribute', { shipmentIds, lines });
  },
};

export interface AggLineRef { shipmentId: number; lineId: number; orderedQty: number; }
export interface ClothingAggProduct {
  stockCode: string;
  stockName: string;
  unit: string;
  clothingType?: number | null;
  totalOrderedQty: number;
  totalDeliveredQty: number;
  refs: AggLineRef[];
}
export interface ClothingAggShipment {
  shipmentId: number;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  projectName: string;
  koliCount?: string | null;
  status: string;
}
export interface ClothingAggregatePickList {
  shipments: ClothingAggShipment[];
  products: ClothingAggProduct[];
}
export interface AggPickInput { stockCode: string; pickedQty: number; differenceReason?: string | null; }

export default clothingPrepService;
