import apiClient from './apiClient';

export interface Driver {
  id: number;
  fullName: string;
  phone: string;
  isActive: boolean;
}

export interface Vehicle {
  id: number;
  plateNumber: string;
  capacity: string | null;
  vehicleType: number;
  vehicleTypeName?: string;
  description: string | null;
  isActive: boolean;
  qrCode?: string | null;
  qrCodeGeneratedAt?: string | null;
}

const transportService = {
  async getDrivers(): Promise<Driver[]> {
    const response = await apiClient.get('/transport/drivers');
    return response.data || [];
  },

  async getActiveDrivers(): Promise<Driver[]> {
    const response = await apiClient.get('/transport/drivers/active');
    return response.data || [];
  },

  async createDriver(data: { fullName: string; phone: string }): Promise<Driver> {
    const response = await apiClient.post('/transport/drivers', data);
    return response.data;
  },

  async updateDriver(data: Driver): Promise<void> {
    await apiClient.put('/transport/drivers', data);
  },

  async deleteDriver(id: number): Promise<void> {
    await apiClient.delete(`/transport/drivers/${id}`);
  },

  async getVehicles(): Promise<Vehicle[]> {
    const response = await apiClient.get('/transport/vehicles');
    return response.data || [];
  },

  async getActiveVehicles(): Promise<Vehicle[]> {
    const response = await apiClient.get('/transport/vehicles/active');
    return response.data || [];
  },

  async createVehicle(data: { plateNumber: string; capacity: string | null; vehicleType: number; description: string | null }): Promise<Vehicle> {
    const response = await apiClient.post('/transport/vehicles', data);
    return response.data;
  },

  async updateVehicle(data: Vehicle): Promise<void> {
    await apiClient.put('/transport/vehicles', data);
  },

  async deleteVehicle(id: number): Promise<void> {
    await apiClient.delete(`/transport/vehicles/${id}`);
  }
};

export default transportService;
