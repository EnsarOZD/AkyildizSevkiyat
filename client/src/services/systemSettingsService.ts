import apiClient from './apiClient';

export interface EmailSettingsDto {
  procurementEmailCc: string | null;
  dispatchEmailCc: string | null;
  dispatchEmailEnabled: boolean;
}

export interface PoCounterDto {
  id: number;
  year: number;
  month: number;
  lastValue: number;
  formattedNumber: string;
  nextNumber: string;
}

export interface DepotSettingsDto {
  depotName: string | null;
  depotAddress: string | null;
  depotLatitude: number | null;
  depotLongitude: number | null;
}

export interface WmsSettingsDto {
  wmsPutawayEnabled: boolean;
  wmsLocationPickingEnabled: boolean;
  wmsBarcodePickingEnabled: boolean;
}

const systemSettingsService = {
  async getDepotSettings(): Promise<DepotSettingsDto> {
    const response = await apiClient.get<DepotSettingsDto>('/system-settings/depot');
    return response.data;
  },

  async saveDepotSettings(data: DepotSettingsDto): Promise<DepotSettingsDto> {
    const response = await apiClient.put<DepotSettingsDto>('/system-settings/depot', {
      depotName: data.depotName,
      depotAddress: data.depotAddress,
      depotLatitude: data.depotLatitude,
      depotLongitude: data.depotLongitude,
    });
    return response.data;
  },

  async getPoCounters(): Promise<PoCounterDto[]> {
    const response = await apiClient.get<PoCounterDto[]>('/system-settings/po-counter');
    return response.data;
  },

  async updatePoCounter(id: number, lastValue: number): Promise<PoCounterDto> {
    const response = await apiClient.put<PoCounterDto>(`/system-settings/po-counter/${id}`, { lastValue });
    return response.data;
  },

  async getEmailSettings(): Promise<EmailSettingsDto> {
    const response = await apiClient.get<EmailSettingsDto>('/system-settings/email');
    return response.data;
  },

  async saveEmailSettings(data: EmailSettingsDto): Promise<EmailSettingsDto> {
    const response = await apiClient.put<EmailSettingsDto>('/system-settings/email', data);
    return response.data;
  },

  async getWmsSettings(): Promise<WmsSettingsDto> {
    const response = await apiClient.get<WmsSettingsDto>('/system-settings/wms');
    return response.data;
  },

  async saveWmsSettings(data: WmsSettingsDto): Promise<WmsSettingsDto> {
    const response = await apiClient.put<WmsSettingsDto>('/system-settings/wms', data);
    return response.data;
  },

  async geocodeAddress(address: string): Promise<{ lat: number; lng: number } | null> {
    // Use backend geocoding proxy via system-settings controller
    // Actually call Google directly from backend - use a dedicated endpoint
    const response = await apiClient.get<{ latitude: number | null; longitude: number | null }>(
      '/system-settings/geocode',
      { params: { address } }
    );
    if (response.data.latitude && response.data.longitude)
      return { lat: response.data.latitude, lng: response.data.longitude };
    return null;
  },
};

export default systemSettingsService;
