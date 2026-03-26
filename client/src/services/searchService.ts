import apiClient from './apiClient';

export interface SearchShipmentDto {
  id: number;
  talepNo?: string;
  projectName: string;
  projectCode: string;
  status: string;
  deliveryDate: string;
}

export interface SearchStockDto {
  id: number;
  stockCode: string;
  stockName: string;
  availableQty: number;
}

export interface SearchProjectDto {
  id: number;
  code: string;
  name: string;
  region?: string;
}

export interface GlobalSearchResult {
  shipments: SearchShipmentDto[];
  stocks: SearchStockDto[];
  projects: SearchProjectDto[];
}

const searchService = {
  async search(q: string): Promise<GlobalSearchResult> {
    const response = await apiClient.get('/search', { params: { q } });
    return response.data;
  },
};

export default searchService;
