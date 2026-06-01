import apiClient from './apiClient';

export interface DriverShipmentDto {
  id: number;
  talepNo?: string;
  deliveryDate: string;
  projectName: string;
  projectAddress?: string;
  teslimAlacakKisiler?: string;
  teslimAlacakTelefon?: string;
  driverName?: string;
  plateNumber?: string;
  status: string;
  deliveredAt?: string;
  deliveryPhotoBase64?: string;
  deliveryPhotoPath?: string;
  lineCount: number;
}

export interface ShipmentLineDto {
  stockCode: string;
  stockName: string;
  orderedQty: number;
  unit: string;
  category: number; // StockCategory enum int value
}

export interface DriverDeliveryPhotoDto {
  id: number;
  photoUrl: string;
  photoIndex: number;
  takenAt: string;
}

export interface StopShipmentDto {
  id: number;
  externalOrderNumber?: string;
  irsaliyeNo?: string;
  status: string;
  lineCount: number;
  deliveryDate: string;
  deliveredAt?: string;
  deliveryRecipient?: string;
  deliveryNote?: string;
  deliveryPhotoBase64?: string;
  deliveryPhotoPath?: string;
  deliveryPhotos?: DriverDeliveryPhotoDto[];
  lines: ShipmentLineDto[];
}

export interface DeliveryStopDto {
  stopNumber: number;
  projectId: number;
  projectName: string;
  projectAddress?: string;
  projectLatitude?: number;
  projectLongitude?: number;
  zoneName?: string;
  deliveryOrder?: number;
  contactName?: string;
  contactPhone?: string;
  shipments: StopShipmentDto[];
  isFullyDelivered: boolean;
  totalLineCount: number;
}

export interface DriverRouteDto {
  stops: DeliveryStopDto[];
  totalStops: number;
  completedStops: number;
  totalShipments: number;
  completedShipments: number;
  mapsRouteUrl?: string;
  zonePreparationId?: number;
}

export interface RouteOrderItemDto {
  projectId: number;
  routeOrder: number;
}

const driverService = {
  async getShipments(): Promise<DriverShipmentDto[]> {
    const response = await apiClient.get('/driver/shipments');
    return response.data;
  },

  async getRoute(): Promise<DriverRouteDto> {
    const response = await apiClient.get('/driver/route');
    return response.data;
  },

  async saveProjectLocation(projectId: number, latitude: number, longitude: number): Promise<void> {
    await apiClient.patch(`/projects/${projectId}/location`, { latitude, longitude });
  },

  async reorderStops(zonePreparationId: number, items: RouteOrderItemDto[]): Promise<void> {
    await apiClient.put('/driver/route/reorder', { zonePreparationId, items });
  },
};

export default driverService;
