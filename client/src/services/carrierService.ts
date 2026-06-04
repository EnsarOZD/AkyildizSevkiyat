import apiClient from './apiClient';

export interface CarrierVehicle {
  id: number;
  plateNumber: string;
  isActive: boolean;
}

export interface Carrier {
  id: number;
  name: string;
  phone?: string | null;
  city?: string | null;
  isActive: boolean;
  vehicles: CarrierVehicle[];
}

export interface CarrierSaveRequest {
  name: string;
  phone?: string | null;
  city?: string | null;
  isActive?: boolean;
  plates: string[];
}

const carrierService = {
  async list(params?: { search?: string; isActive?: boolean }): Promise<Carrier[]> {
    const res = await apiClient.get<Carrier[]>('/carriers', { params });
    return res.data;
  },

  async create(data: CarrierSaveRequest): Promise<number> {
    const res = await apiClient.post<{ id: number }>('/carriers', data);
    return res.data.id;
  },

  async update(id: number, data: CarrierSaveRequest): Promise<void> {
    await apiClient.put(`/carriers/${id}`, data);
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`/carriers/${id}`);
  },
};

export default carrierService;
