import apiClient from './apiClient';

export type UserRole = 'Admin' | 'Accounting' | 'Warehouse' | 'Manager' | 'Driver';

export interface UserListItem {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role: number;
}

export interface UpdateUserRequest {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
}

const userService = {
  async getAll(): Promise<UserListItem[]> {
    const response = await apiClient.get('/users');
    return response.data || [];
  },

  async create(data: CreateUserRequest): Promise<number> {
    const response = await apiClient.post('/users', data);
    return response.data;
  },

  async update(data: UpdateUserRequest): Promise<void> {
    await apiClient.put(`/users/${data.id}`, data);
  },

  async toggleActive(id: number, isActive: boolean): Promise<void> {
    await apiClient.post(`/users/${id}/toggle-active`, { isActive });
  },

  async resetPassword(id: number, newPassword: string): Promise<void> {
    await apiClient.post(`/users/${id}/reset-password`, { newPassword });
  }
};

export default userService;
