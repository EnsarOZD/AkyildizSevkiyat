import apiClient from './apiClient';

export type OperationType = 0 | 1; // 0 = Catering, 1 = Clothing

export interface Customer {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  operationType: OperationType;
  netsisCariKodu: string | null;
  netsisTeslimCariKodu: string | null;
  address: string | null;
  cityName: string | null;
  districtName: string | null;
  latitude: number | null;
  longitude: number | null;
  defaultContactName: string | null;
  defaultContactPhone: string | null;
}

export interface CustomerInput {
  name: string;
  netsisCariKodu: string;
  netsisTeslimCariKodu?: string | null;
  operationType?: OperationType;
  address?: string | null;
  cityName?: string | null;
  districtName?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  defaultContactName?: string | null;
  defaultContactPhone?: string | null;
}

export interface CustomerQueryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  showInactive?: boolean;
}

export interface PaginatedCustomers {
  items: Customer[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

const customerService = {
  async getAll(params: CustomerQueryParams = {}): Promise<PaginatedCustomers> {
    const response = await apiClient.get('/customers', { params });
    const d = response.data;
    return {
      items: d.items || d.Items || [],
      pageIndex: d.pageIndex ?? d.PageIndex ?? 1,
      totalPages: d.totalPages ?? d.TotalPages ?? 1,
      totalCount: d.totalCount ?? d.TotalCount ?? 0,
      hasPreviousPage: d.hasPreviousPage ?? d.HasPreviousPage ?? false,
      hasNextPage: d.hasNextPage ?? d.HasNextPage ?? false,
    };
  },

  async getById(id: number): Promise<Customer> {
    const response = await apiClient.get(`/customers/${id}`);
    return response.data;
  },

  async create(input: CustomerInput): Promise<number> {
    const response = await apiClient.post('/customers', input);
    return response.data?.id ?? response.data?.Id;
  },

  async update(id: number, input: CustomerInput): Promise<void> {
    await apiClient.put(`/customers/${id}`, { id, ...input });
  },

  async toggleActive(id: number, isActive: boolean): Promise<void> {
    await apiClient.patch(`/customers/${id}/toggle-active`, null, { params: { isActive } });
  },
};

export default customerService;
