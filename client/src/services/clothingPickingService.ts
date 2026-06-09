import apiClient from './apiClient';

export interface PickingQueueItem {
  shipmentId: number;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  projectCode: string;
  projectName: string;
  pickingGroupId?: number | null;
  queueOrder: number;
  lineCount: number;
  reservedForUserId?: number | null;
  reservedForMe: boolean;
  assignedPickerId?: number | null;
  assignedPickerName?: string | null;
  status: string;
  pickingMode?: number | null;
}

export interface PickingQueue {
  claimable: PickingQueueItem[];
  mine: PickingQueueItem[];
}

export interface ShipmentContainer {
  containerAssignmentId: number;
  containerId: number;
  code: string;
  containerType: number;
  assignedAt: string;
}

export interface ScanContainerResult {
  containerAssignmentId: number;
  containerId: number;
  code: string;
  containerType: number;
}

export interface ByContainerShipment {
  shipmentId: number;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  projectName: string;
  status: string;
  pickingMode?: number | null;
  pickingCompleted: boolean;
  paused: boolean;
  boxCount?: number | null;
  closed: boolean;
  otherContainerCodes: string[];
}

export interface ByContainer {
  containerCode: string;
  containerType: number;
  shipments: ByContainerShipment[];
}

export interface PickLineInput {
  shipmentLineId: number;
  deliveredQty: number;
  differenceReason?: string | null;
  note?: string | null;
}

// PickingMode: 0=Cart, 1=Pallet, 2=Handheld ; PackageType: 0=Koli, 1=Poset
export const PickingModeLabels: Record<number, string> = { 0: 'Araba', 1: 'Palet', 2: 'El' };

export interface PickingOverviewItem {
  shipmentId: number;
  externalOrderNumber?: string | null;
  talepNo?: string | null;
  projectCode: string;
  projectName: string;
  pickingGroupId?: number | null;
  queueOrder: number;
  reservedForUserId?: number | null;
  assignedPickerId?: number | null;
  assignedPickerName?: string | null;
  status: string;
  pickingMode?: number | null;
  claimedOutOfOrder: boolean;
  paused: boolean;
  pickingCompleted: boolean;
  openContainerCount: number;
  lineCount: number;
}
export interface PickingOverviewGroup { id: number; name: string; sortOrder: number; isActive: boolean; }
export interface PickingOverview { groups: PickingOverviewGroup[]; items: PickingOverviewItem[]; }

const clothingPickingService = {
  // Yönetici panosu
  overview: () => apiClient.get('/clothing-picking/overview').then(r => r.data as PickingOverview),

  // Kuyruk
  queue: (groupId?: number | null) =>
    apiClient.get('/clothing-picking/queue', { params: { groupId } }).then(r => r.data as PickingQueue),

  // Claim / Unclaim
  claim: (id: number) => apiClient.post(`/clothing-picking/${id}/claim`).then(r => r.data),
  unclaim: (id: number, reason: string) => apiClient.post(`/clothing-picking/${id}/unclaim`, { reason }),

  // Toplama akışı
  setMode: (id: number, mode: number) => apiClient.post(`/clothing-picking/${id}/mode`, { mode }),
  containers: (id: number) => apiClient.get(`/clothing-picking/${id}/containers`).then(r => r.data as ShipmentContainer[]),
  scanContainer: (id: number, code: string) =>
    apiClient.post(`/clothing-picking/${id}/scan-container`, { code }).then(r => r.data as ScanContainerResult),
  releaseContainer: (containerAssignmentId: number, reason: string) =>
    apiClient.post('/clothing-picking/release-container', { containerAssignmentId, reason }),
  saveProgress: (id: number, lines: PickLineInput[]) =>
    apiClient.post(`/clothing-picking/${id}/save-progress`, { lines }),
  completePicking: (id: number, lines: PickLineInput[], confirmContainers: boolean, palletCount?: number | null) =>
    apiClient.post(`/clothing-picking/${id}/complete-picking`, { lines, confirmContainers, palletCount }),
  pause: (id: number) => apiClient.post(`/clothing-picking/${id}/pause`),
  resume: (id: number) => apiClient.post(`/clothing-picking/${id}/resume`),

  // Kapama + etiket
  completeClosing: (id: number, boxCount: number, packageType: number, note?: string | null) =>
    apiClient.post(`/clothing-picking/${id}/complete-closing`, { boxCount, packageType, note }),
  labelHandwritten: (id: number) => apiClient.post(`/clothing-picking/${id}/label-handwritten`),
  byContainer: (code: string) =>
    apiClient.get(`/clothing-picking/by-container/${encodeURIComponent(code)}`).then(r => r.data as ByContainer),

  // Yönetici
  assignGroup: (shipmentIds: number[], pickingGroupId: number | null) =>
    apiClient.post('/clothing-picking/assign-group', { shipmentIds, pickingGroupId }),
  reorder: (orderedShipmentIds: number[]) =>
    apiClient.post('/clothing-picking/reorder', { orderedShipmentIds }),
  reserve: (id: number, reservedForUserId: number | null) =>
    apiClient.post(`/clothing-picking/${id}/reserve`, { reservedForUserId }),
};

export default clothingPickingService;
