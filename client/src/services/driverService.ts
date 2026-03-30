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
  lineCount: number;
}

export interface ShipmentLineDto {
  stockCode: string;
  stockName: string;
  orderedQty: number;
  unit: string;
}

export interface StopShipmentDto {
  id: number;
  talepNo?: string;
  irsaliyeNo?: string;
  status: string;
  lineCount: number;
  deliveryDate: string;
  teslimAlacakKisiler?: string;
  teslimAlacakTelefon?: string;
  deliveredAt?: string;
  deliveryRecipient?: string;
  deliveryNote?: string;
  deliveryPhotoBase64?: string;
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
};

export default driverService;
