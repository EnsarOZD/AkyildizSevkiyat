import apiClient from './apiClient';

export interface DepotSettingsDto {
  depotName: string | null;
  depotAddress: string | null;
  depotLatitude: number | null;
  depotLongitude: number | null;
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
