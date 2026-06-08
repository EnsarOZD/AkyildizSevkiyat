import apiClient from './apiClient';

export interface ClothingKeyword {
  id: number;
  keyword: string;
  color: string;
  isActive: boolean;
  sortOrder: number;
}

const clothingKeywordService = {
  async getAll(activeOnly = false): Promise<ClothingKeyword[]> {
    const res = await apiClient.get('/clothing-keywords', { params: { activeOnly } });
    return res.data;
  },

  async save(data: { id?: number | null; keyword: string; color: string; isActive: boolean; sortOrder: number }): Promise<{ id: number }> {
    const res = await apiClient.post('/clothing-keywords', data);
    return res.data;
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`/clothing-keywords/${id}`);
  },
};

export default clothingKeywordService;
