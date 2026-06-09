import apiClient from './apiClient';

export interface Shortage {
  id: number;
  shipmentId: number;
  externalOrderNumber?: string | null;
  shipmentLineId?: number | null;
  stockMasterId?: number | null;
  stockCode: string;
  stockName: string;
  projectId: number;
  projectName: string;
  qty: number;
  status: number;      // 0 Pending,1 DispatchRequested,2 Picked,3 Shipped,9 Cancelled
  statusName: string;
  note?: string | null;
  createdAt: string;
  followupShipmentId?: number | null;
}

export const ShortageStatusLabels: Record<number, string> = {
  0: 'Beklemede', 1: 'Gönderim İstendi', 2: 'Toplandı', 3: 'Gönderildi', 9: 'İptal',
};

const shortageService = {
  list: (status?: number | null, projectId?: number | null) =>
    apiClient.get('/shortages', { params: { status, projectId } }).then(r => r.data as Shortage[]),
  dispatch: (shortageIds: number[]) =>
    apiClient.post('/shortages/dispatch', { shortageIds }).then(r => r.data as { shipmentIds: number[] }),
  cancel: (id: number, reason: string) =>
    apiClient.post(`/shortages/${id}/cancel`, { reason }),
};

export default shortageService;
