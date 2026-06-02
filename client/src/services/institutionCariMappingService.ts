import apiClient from './apiClient';

export interface InstitutionCariMapping {
  id: number;
  institutionCode: string;
  netsisCariKodu: string;
  description: string | null;
  isActive: boolean;
}

export interface CreateMappingInput {
  institutionCode: string;
  netsisCariKodu: string;
  description?: string | null;
}

export interface UpdateMappingInput extends CreateMappingInput {
  id: number;
  isActive: boolean;
}

export interface ProjectChangePreview {
  projectId: number;
  projectCode: string;
  projectName: string;
  institutionCode: string | null;
  currentNetsisCariKodu: string | null;
  newNetsisCariKodu: string | null;
  mappingDescription: string | null;
}

export interface ApplyMappingsResult {
  totalProjectsScanned: number;
  affectedCount: number;
  unchangedCount: number;
  noMappingCount: number;
  changes: ProjectChangePreview[];
  withoutMapping: ProjectChangePreview[];
  dryRun: boolean;
}

const institutionCariMappingService = {
  async getAll(showInactive = false): Promise<InstitutionCariMapping[]> {
    const res = await apiClient.get('/institution-cari-mappings', { params: { showInactive } });
    return res.data || [];
  },

  async create(input: CreateMappingInput): Promise<number> {
    const res = await apiClient.post('/institution-cari-mappings', input);
    return res.data?.id ?? res.data?.Id;
  },

  async update(id: number, input: UpdateMappingInput): Promise<void> {
    await apiClient.put(`/institution-cari-mappings/${id}`, { ...input, id });
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`/institution-cari-mappings/${id}`);
  },

  async applyToProjects(dryRun: boolean): Promise<ApplyMappingsResult> {
    const res = await apiClient.post('/institution-cari-mappings/apply-to-projects', null, {
      params: { dryRun },
    });
    return res.data;
  },
};

export default institutionCariMappingService;
